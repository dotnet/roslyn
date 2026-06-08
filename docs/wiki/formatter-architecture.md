# Roslyn C# Formatter Architecture

This document captures the architecture walkthrough discussed in formatter onboarding conversations and expands the existing [`Notes-on-formatting.md`](./Notes-on-formatting.md) with a system-level map.

## End-to-end pipeline

The formatter flow is:

1. `.editorconfig` / workspace options
2. `FormattingOptions2` / `CSharpFormattingOptions2`
3. `LineFormattingOptions` / `SyntaxFormattingOptions`
4. `CSharpSyntaxFormattingOptions` (flags)
5. Rule chain emits operations
6. Engine applies operations over token pairs
7. `TextChange[]` or formatted root

## Options and option materialization

- `Compiler/Core/Formatting/LineFormattingOptions.cs` and `SyntaxFormattingOptions.cs` represent language-agnostic options consumed by engine/rules.
- `Compiler/CSharp/Formatting/CSharpSyntaxFormattingOptions.cs` projects C#-specific options into strongly typed formatting flags.
- `Compiler/CSharp/Formatting/CSharpFormattingOptions2.cs` (+ `.Parsers.cs`) defines `csharp_*` editorconfig keys and parsers.
- `*OptionsProviders.cs` in Workspaces read options and materialize the records used by formatting APIs.

## Orchestration layer

- `Compiler/Core/Formatting/ISyntaxFormatting.cs` defines per-language formatting contract.
- `Compiler/Core/Formatting/AbstractSyntaxFormatting.cs` coordinates range normalization, token-pair conversion, per-range format calls, and aggregation.
- `Compiler/CSharp/Formatting/CSharpSyntaxFormatting.cs` provides the C# rule chain and creates `CSharpFormatEngine`.
- `Compiler/Core/Formatting/IFormattingResult.cs` and result implementations expose changes/root.

## Engine core

- `Compiler/Core/Formatting/Engine/AbstractFormatEngine.cs` is the collect/apply core.
- `AbstractFormatEngine.OperationApplier.cs` translates operations to concrete whitespace changes.
- `TokenStream.cs` and related iterator/change types track original trivia and accumulated edits per adjacent token pair.
- `TreeData*` types support formatting with real text and textless/generated trees, plus structured trivia recursion.
- `ChainedFormattingRules.cs` materializes chain-of-responsibility dispatch over rules.

Important precedence behavior: if a line operation applies for a token pair, spacing for that pair is skipped.

## Operations and rules

The formatting instruction set consists of six operations:

- `AdjustSpacesOperation` (token pair)
- `AdjustNewLinesOperation` (token pair)
- `IndentBlockOperation` (span)
- `AlignTokensOperation` (span)
- `AnchorIndentationOperation` (span)
- `SuppressOperation` (span)

Rule infrastructure lives under `Compiler/Core/Formatting/Rules/` (`AbstractFormattingRule`, `Next*Action` continuations, `NoOpFormattingRule`, `BaseIndentationFormattingRule`).

### C# rule chain order

Defined in `Compiler/CSharp/Formatting/CSharpSyntaxFormatting.cs`:

1. `WrappingFormattingRule`
2. `SpacingFormattingRule`
3. `NewLineUserSettingFormattingRule`
4. `IndentUserSettingsFormattingRule`
5. `ElasticTriviaFormattingRule`
6. `EndOfFileTokenFormattingRule`
7. `StructuredTriviaFormattingRule`
8. `IndentBlockFormattingRule`
9. `SuppressFormattingRule`
10. `AnchorIndentationFormattingRule`
11. `QueryExpressionFormattingRule`
12. `TokenBasedFormattingRule`
13. `DefaultOperationProvider`

For per-token-pair queries, the first rule to return a non-null operation wins.

## Formatting context and indentation model

`Compiler/Core/Formatting/Context/FormattingContext.cs` keeps mutable interval trees for indentation, relative indentation, suppressions (wrapping/spacing/formatting), and anchors.

`BottomUpBaseIndentationFinder.cs` computes indentation bottom-up from enclosing indent operations and `IndentationSize`. Smart-indent reuses this logic for on-enter cursor positioning.

## Trivia subsystem

Core abstractions are under `Compiler/Core/Formatting/Engine` + `.../TriviaEngine`; C# specializations are under `Compiler/CSharp/Formatting/Engine/Trivia`.

Key concepts:

- `TriviaData` models the gap between adjacent tokens.
- Simple whitespace (`Whitespace`, `ModifiedWhitespace`, `FormattedWhitespace`) is handled separately from complex/comment-containing trivia.
- `TriviaDataFactory.CodeShapeAnalyzer` determines whether it is safe to normalize single-line or multi-line trivia.
- `CSharpTriviaFormatter` re-indents comments/directives/doc-comment exteriors when needed.

## Roslyn-specific concepts

### Full-fidelity syntax with trivia

Roslyn preserves all spaces/newlines/comments as trivia on tokens. This enables minimal-edit formatting (`TextChange[]`) instead of whole-document regeneration.

### Elastic trivia

Code generation inserts elastic trivia as formatting placeholders. `ElasticTriviaFormattingRule` and `TreatAsElastic` allow normalization where generators intentionally left layout undecided.

## Comparison summary

- Roslyn is edit-based and range-formatting-first.
- Roslyn is not width-budget/line-wrap optimizing in the formatter engine.
- Roslyn does not use a document IR like Prettier’s `Doc`; it uses imperative formatting operations plus context.
- Trivia + elastic trivia are central to Roslyn’s formatter behavior.

## Suggested reading order

1. `docs/wiki/Notes-on-formatting.md`
2. `Compiler/Core/Formatting/Engine/AbstractFormatEngine.cs` (`Format()`)
3. `Compiler/Core/Formatting/Rules/AbstractFormattingRule.cs` + `Compiler/CSharp/Formatting/Rules/SpacingFormattingRule.cs`
4. `Compiler/Core/Formatting/Rules/Operations/*.cs`
5. `Compiler/Core/Formatting/Context/FormattingContext.cs` + `BottomUpBaseIndentationFinder.cs`
6. `Compiler/Core/Formatting/Engine/TriviaData.cs` + C# trivia factory/analyzer
7. Tests in `src/Workspaces/CSharpTest/Formatting/`

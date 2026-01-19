# Virtual Character System Specification

## 1. Overview & Motivation

### 1.1 The Problem

Embedded languages within string literals present a fundamental challenge: they need precise position tracking, but
string escaping collapses source text in non-trivial ways. IDE features like colorization, brace matching, completion,
and diagnostics require bidirectional mapping between logical characters and their source representations.

Without this mapping, features can only operate on the processed string value (`token.ValueText`), losing the connection
to what the user actually typed.

### 1.2 Examples Demonstrating the Need

**Normal Strings**
- Source: `"Hello\tWorld"`
- Logical: `"Hello	World"` (with actual tab character)
- Challenge: `\t` (two chars) → tab (one logical char)
- Need: Map tab back to its source span [5, 7)

**Verbatim Strings**
- Source: `@"He said ""Hello"""`
- Logical: `"He said "Hello""`
- Challenge: `""` (two chars) → `"` (one logical char)

**Unicode Escapes**
- Source: `"Test\u0041B"`
- Logical: `"TestAB"`
- Challenge: `\u0041` (six chars) → `A` (one logical char)

**XML Documentation**
- Source: `<code>int x &lt; 5;</code>`
- Logical: `"int x < 5;"`
- Challenge: `&lt;` (four chars) → `<` (one logical char)

### 1.3 Supported Embedded Languages

The VirtualChar system enables IDE features for multiple embedded language scenarios:

- **Regex**: Pattern matching strings in `Regex` APIs
- **JSON**: Data literals (strict RFC8259 and JSON.NET variants)
- **C#**: String literals containing C# code
- **C#-Test**: Special test markup variant with `[|...|]` and `{|Name:...|}`
- **C# in documentation**: `<code>` blocks in XML doc comments

All benefit from the same VirtualChar foundation.

### 1.4 Solution Architecture

VirtualChars solve this problem by providing:

1. **1:1 mapping**: Each logical character maps to exactly one VirtualChar
2. **Bidirectional navigation**: Logical position ↔ source span
3. **Escape abstraction**: Consumers work with logical characters; VirtualChar handles source complexity
4. **Foundation for features**: Syntax trees, diagnostics, and IDE features built on this abstraction

## 2. Core Abstractions

### 2.1 VirtualCharGreen & VirtualChar

The VirtualChar system follows Roslyn's green/red architecture pattern for memory efficiency and immutability.

#### VirtualCharGreen (Position-Independent)

**Design characteristics**:
- **Immutable**: Never modified after construction
- **Position-independent**: Offset is relative to token start, not absolute
- **Shareable**: Can be reused across different contexts
- **Memory-optimized**: Packs offset and width into single integer

**Key fields**:
- `char Char`: The logical character
- `int _offsetAndWidth`: Packed offset (28 bits) + width (4 bits)

**Packing details**:
- Width limited to 4 bits (max value: 15)
- Sufficient for longest escape: `\uXXXX\uXXXX` (12 characters)
- Offset uses remaining 28 bits (supports tokens up to 268M chars)

**Examples**:
- `'a'` in `"abc"`: offset=1, width=1
- Tab in `"x\ty"`: char=`\t`, offset=2, width=2
- `'A'` in `"x\u0041y"`: char=`A`, offset=2, width=6

#### VirtualChar (Position-Aware)

**Red wrapper properties**:
- Contains `VirtualCharGreen Green` + `int TokenStart`
- **Absolute positioning**: TokenStart provides file-level position context
- **Computed span**: Combines token position with green offset/width
- **Lightweight**: Just green reference + one integer
- **Implicit conversion**: Can be used directly as `char`

**Design rationale**: Following Roslyn's green/red split:
- Green: Immutable, shareable, position-independent (efficient for caching)
- Red: Adds positional context on-demand (efficient for consumption)

### 2.2 VirtualCharSequence & VirtualCharGreenSequence

#### Sequence Abstraction

Represents the complete processed contents of a string token as a sequence of VirtualChars.

**Structure**:
- Green version: position-independent, contains `Chunk` + `TextSpan` for slicing
- Red version: adds `TokenStart` for absolute positioning

**Slicing support**: Efficient subsequence extraction without copying
- Example: `"\"Hello\""` → slice `[1..^1]` → `"Hello"` (no allocation)

#### Memory Optimization: Chunk Architecture

Two implementations optimized for different scenarios:

**StringChunk** (Common case: no escapes)
- For tokens like `"Hello World"` (no escape sequences)
- **Zero allocation**: No array materialized
- **Direct indexing**: VirtualChar created on-demand from string
- **Memory savings**: Typical case in most code

**ImmutableSegmentedListChunk** (Escapes present)
- For tokens like `"Hello\tWorld"` (contains escapes)
- **Materialized storage**: Array holds pre-computed VirtualCharGreens
- **Preserves escape info**: Each element stores width for original escape sequence
- **Only when needed**: Allocated only when escapes detected

**Performance impact**: 
- Unescaped strings: No heap allocation beyond the string token itself
- Escaped strings: Single allocation for array
- Slicing: Both support efficient slicing without copying

### 2.3 Critical Invariants

#### Invariant 1: 1:1 Correspondence
Each character in `token.ValueText` maps to exactly one `VirtualChar` in the sequence.

#### Invariant 2: Contiguous Coverage
The union of all `VirtualChar.Span`s covers the entire `token.Text` excluding quotes.

#### Invariant 3: Adjacency (Non-Raw Strings)
For regular strings, `VirtualChar[i].Span.End == VirtualChar[i+1].Span.Start`

**Exception**: Multi-line raw string literals may have gaps (whitespace/newlines stripped)

#### Invariant 4: Well-Formed Input Only
Conversion succeeds only for tokens without diagnostics and well-formed escape sequences.

**Failure cases** (returns `default`):
- Token has any diagnostics
- Not a string literal token
- Contains multi-char escape sequences (rare edge case)

### 2.4 IVirtualCharService

Language-specific interface for converting string tokens to VirtualChar sequences.

**Key methods**:
- `VirtualCharSequence TryConvertToVirtualChars(SyntaxToken token)`
- `bool TryGetEscapeCharacter(VirtualChar ch, out char escapeChar)`

**Responsibilities**:
1. Token validation (verify string literal without diagnostics)
2. Escape processing (language-specific rules)
3. VirtualChar generation (maintaining invariants)
4. Reverse mapping (logical char → escape form)

**Implementations**:
- `CSharpVirtualCharService`: Handles C# string escaping rules
- `VisualBasicVirtualCharService`: Handles VB string escaping rules

## 3. Embedded Language Detection

The system must identify which string literals contain embedded languages and which specific language they contain.

### 3.1 Detection Strategies

#### Strategy 1: `[StringSyntax]` Attribute

The most explicit detection mechanism uses .NET 7+ `System.Diagnostics.CodeAnalysis.StringSyntaxAttribute`.

**Example**: `void ProcessRegex([StringSyntax("Regex")] string pattern)`

**Locations checked**:
- Method/constructor parameters
- Field declarations
- Property declarations
- Attribute constructor arguments

**Algorithm**: Parse argument → resolve parameter symbol → check for attribute → extract language identifier

#### Strategy 2: Comment Annotations

Lightweight annotation using special comment syntax: `// lang=<identifier>[,<option1>,<option2>,...]`

**Example**:
```csharp
// lang=regex
var pattern = "\\d+";
```

**Scope rules**:
- Applies to next statement or declaration
- Scans leading trivia of statement
- Also checks trailing trivia of previous token

**Options support**: Comma-separated options passed to parser configuration

**Precedence**: Comments override attribute detection (allows local override)

#### Strategy 3: Well-Known APIs

Recognition of framework types and methods known to accept embedded language strings.

**Regex APIs**: `Regex.IsMatch`, `Regex.Replace`, `new Regex(...)`, etc.

**Recognition logic**:
1. Maintain hash set of method names
2. Verify symbol belongs to known type
3. Find parameter named `"pattern"` and match argument

**API registry**: Built at compilation level from type members

#### Strategy 4: Interpolation Format String Analysis

Special handling for format strings in interpolated string expressions.

**Example**: `$"{date:yyyy-MM-dd}"`

**Detection flow**:
1. Identify format portion (`:yyyy-MM-dd`)
2. Get type of expression (`DateTime`)
3. Find `IFormattable.ToString` implementation
4. Check first parameter for `[StringSyntax]` attribute

### 3.2 Language Detector Architecture

Detectors are compilation-scoped services that efficiently identify embedded language tokens.

**Key components**:
- Type symbol caching (e.g., `Regex` type resolved once)
- Method name caching (well-known methods computed once)
- No tree caching (trees built on-demand for visible tokens only)

**Generic infrastructure**: `EmbeddedLanguageDetector` with language identifiers and detection strategies

### 3.3 Detection Flow Examples

**Direct Parameter Annotation**: String literal → argument → parameter with `[StringSyntax]` → detected

**Local Variable Flow Tracking**: Assignment (no detection) → usage in well-known API → backtrack to assignment → mark
as embedded language

**Field with Const/Readonly**: Attribute on field → scan descendants for references → mark usage sites

**Comment Override**: Comment takes precedence over all other detection strategies

### 3.4 Detection Strategy Precedence

1. **Comment annotations** (highest precedence)
2. **StringSyntax attributes**
3. **Well-known API recognition**
4. **Interpolation format analysis**

Rationale: Comments allow local overrides for edge cases

## 4. Embedded Language Syntax Trees

Embedded language parsers produce syntax trees that mirror Roslyn's core design principles while remaining independent
and language-specific.

### 4.1 Architecture Parallels with Roslyn

| Roslyn Construct | Embedded Language Equivalent |
|------------------|------------------------------|
| `SyntaxToken` | `EmbeddedSyntaxToken<TSyntaxKind>` |
| `SyntaxNode` | `EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>` |
| `SyntaxTrivia` | `EmbeddedSyntaxTrivia<TSyntaxKind>` |
| `SyntaxTree` | `EmbeddedSyntaxTree<...>` |
| Green/Red split | **VirtualChar only** (not yet for nodes) |

**Design principles shared with Roslyn**:
- Immutability
- Full fidelity (represent all source, including errors)
- Uniform child access via `ChildAt(index)`
- Span tracking

**Key difference**: Currently no green/red split for embedded syntax nodes/tokens (only VirtualChar has this
separation). See §7.1 for future optimization.

### 4.2 EmbeddedSyntaxToken

Represents a token within an embedded language, backed by VirtualChars.

**Core properties**:
- `TSyntaxKind Kind`: Language-specific token type
- `VirtualCharSequence VirtualChars`: The actual characters
- `ImmutableArray<EmbeddedSyntaxTrivia> LeadingTrivia/TrailingTrivia`
- `ImmutableArray<EmbeddedDiagnostic> Diagnostics`
- `object Value`: Optional semantic interpretation (parsed numbers, capture names, etc.)

**Trivia handling**: Limited compared to Roslyn
- Regex: Whitespace/comments only in `IgnorePatternWhitespace` mode
- JSON: Whitespace, single-line (`//`), multi-line (`/* */`) comments

**Position**: Derived from VirtualChars (first char start to last char end)

### 4.3 EmbeddedSyntaxNode

Abstract base for all non-terminal nodes in embedded syntax trees.

**Key characteristics**:
- Uniform child access: `ChildAt(index)` returns nodes or tokens
- No parent pointers (simplifies immutability, not needed yet)
- No Update methods (trees built once, never modified)
- Span computation derived from children on-demand

**Enumeration**: Supports `foreach` over children

### 4.4 EmbeddedSeparatedSyntaxNodeList

Specialized structure for comma-delimited (or bar-delimited) constructs.

**Storage pattern**: Alternating nodes and separators in `ImmutableArray`
- Even indices: nodes
- Odd indices: separator tokens

**Indexer**: Returns node at `index * 2` (skips separators)

**Example**: JSON array `[1, 2, 3]` stored as `[Node₁, Comma, Node₂, Comma, Node₃]`

### 4.5 Complete Tree Structure

**Components**:
- `VirtualCharSequence Text`: Source of truth for positions
- `TCompilationUnitSyntax Root`: Top-level node (always present)
- `ImmutableArray<EmbeddedDiagnostic> Diagnostics`: All errors/warnings (deduplicated)

**Concrete instantiations**:
- `RegexTree`: Includes `CaptureNames` and `CaptureNumbers` dictionaries
- `JsonTree`: Pure tree without additional properties

### 4.6 Parsing Pipeline

**Flow**: `SyntaxToken` → `IVirtualCharService` → `VirtualCharSequence` → `Parser.TryParse()` → `EmbeddedSyntaxTree`

**Parser characteristics**:
- **Always succeed** (except stack overflow)
- **Full fidelity**: Every VirtualChar represented in tree
- **Error recovery**: Missing tokens synthesized, diagnostics attached
- **Diagnostic matching**: Replicate native parser error messages exactly

## 5. Feature Integration

### 5.1 Brace Matching

Highlights matching delimiters when cursor is adjacent.

**Algorithm**:
1. Position → VirtualChar (via `tree.Text.Find(position)`)
2. VirtualChar → Node containing it (recursive descent)
3. Extract open/close tokens from grouping/character class node
4. Return span pair for highlighting

**Supported constructs**: Parentheses, brackets, braces, comment delimiters

### 5.2 Classification & Colorization

Provides syntax highlighting within embedded language strings.

**Process**:
1. Walk all tokens in tree
2. Map token kind → classification type (e.g., `RegexKind.NumberToken` → `"regex - quantifier"`)
3. Extract VirtualChar spans → source TextSpans
4. Publish classification spans to IDE

**Granularity**: Individual constructs colored distinctly (escape sequences, keywords, operators, etc.)

### 5.3 Diagnostics

Errors and warnings reported with precise source spans.

**Collection**: Diagnostics attached during parsing to tokens/trivia, then aggregated into tree

**Deduplication**: Same diagnostic at same position appears once

**IDE integration**: VirtualChar spans map directly to squiggle locations

### 5.4 Completion

Context-sensitive suggestions within embedded language strings.

**Trigger scenarios**:
- After `\` in regex: offer escape sequences
- After `\k<` or `\<`: offer capture names from tree
- Inside JSON: offer property names, keywords (future)

**VirtualChar role**: Precise replacement span calculation from character positions

### 5.5 Reference Highlighting

Highlights all references to a symbol (e.g., regex capture group references).

**Implementation**: Find symbol at position → locate all references in tree → return spans


## 6. Language-Specific Examples

### 6.1 Regular Expressions

**Parser structure**: `RegexCompilationUnit` → `RegexAlternationNode` → `RegexSequenceNode` → expressions

**Key node types**:
- Grouping: `(?:...)`, `(?<name>...)`, `(?=...)`, etc.
- Character class: `[a-z]`, `[^0-9]`
- Quantifiers: `*`, `+`, `?`, `{n,m}`
- Anchors: `^`, `$`, `\b`, `\A`, `\z`
- Escapes: `\t`, `\d`, `\w`, `\p{Lu}`

**Example**: `"\\d+"` → `CharacterClassEscapeNode(\d)` + `OneOrMoreQuantifierNode(+)`

**Capture tracking**: Tree maintains dictionaries mapping capture names/numbers to their definition spans

### 6.2 JSON

**Parser structure**: `JsonCompilationUnit` → `JsonObjectNode` / `JsonArrayNode` → properties/elements

**Value types**: Literals (string, number, true, false, null), Objects, Arrays

**Example**: `"{\"key\": 123}"` → `JsonObjectNode` with `JsonPropertyNode` containing string name and number value

**Separated lists**: Properties in objects, elements in arrays use `EmbeddedSeparatedSyntaxNodeList`

### 6.3 C# and C#-Test

#### C# in String Literals

Used for code generation templates, dynamic compilation, IDE scenarios.

**Detection**: `[StringSyntax("C#")]` or `// lang=C#`

#### C# in Documentation Comments

C# code within `<code>` blocks in XML documentation.

**Processing**: Extract content → process XML entities (`&lt;` → `<`) → create VirtualChar sequence → parse as C#

#### C#-Test: Test Markup Language

Special variant for writing Roslyn IDE tests with embedded annotations.

**Features**:
- **Span markers**: `[|...|]` marks text spans
- **Named spans**: `{|Name:...|}`  marks spans with identifiers
- **Position markers**: `$$` marks cursor positions

**Processing**:
1. Scan for markup delimiters
2. Extract markup spans and names
3. Build VirtualChar sequence excluding markup characters
4. Map spans back to positions in unmarked text

**Use cases**: IntelliSense tests, navigation tests, refactoring tests, diagnostic tests

## 7. Future Directions

### 7.1 Green/Red Split for Embedded Syntax Trees

**Current state**: Only VirtualChar has green/red separation

**Potential optimization**: Extend green/red pattern to entire embedded syntax trees

**Benefits**:
- Incremental reuse when editing (unchanged strings reuse green nodes)
- Memory sharing (green tree parsed once, shared across usage sites)
- Reduced parsing cost (avoid re-parsing on position-only changes)

**Current status**: Not implemented because features only process visible strings (small working set), edits rarely
affect multiple strings

**Trigger**: Would become valuable if features expand to whole-file analysis

### 7.2 Additional Language Support

**Requirements for new language**:
1. Parser accepting `VirtualCharSequence`
2. Position preservation (concrete syntax tree, not AST)
3. Full fidelity representation
4. Precise diagnostic spans

**Candidate languages**: SQL, GraphQL, Markdown, CSS/SCSS, YAML/TOML

**Integration path**:
1. Implement `IVirtualCharService` (if unique escaping needed)
2. Write parser: `VirtualCharSequence → EmbeddedSyntaxTree`
3. Register language detector
4. Implement feature providers (classification, brace matching, completion, diagnostics)

### 7.3 Cross-Language Analysis

**Scenario**: Nested embedded languages (e.g., SQL containing JSON)

**Challenge**: Multiple escaping layers (C# + SQL + JSON)

**Potential solution**: Compose VirtualChar sequences across language boundaries

**Current status**: Not supported, but architecture could accommodate

### 7.4 Performance Enhancements

**Caching opportunities**:
- Parser result caching (token → tree, invalidate on edit)
- VirtualChar sequence caching (avoid re-conversion)
- Compilation-level detector caching (already implemented)

**Streaming parsing**: For very large string literals (not common)

**Lazy tree construction**: Build on-demand rather than eagerly

### 7.5 Enhanced Error Recovery

**Improvements**:
- Partial tree reuse during incremental edits
- Better error messages with suggestions
- Fix-it hints in diagnostics

### 7.6 Testing Infrastructure

**Helpers**: VirtualChar sequence assertions, tree comparison utilities, diagnostic verification

## 8. Design Principles Summary

### 8.1 Alignment with Roslyn

**Shared patterns**: Immutability, full fidelity, span precision, uniform traversal, factory patterns

**Green/red pattern**: Currently VirtualChar only; future expansion opportunity

### 8.2 Performance-Conscious Design

**Memory optimizations**: StringChunk zero-allocation, bit packing, lazy construction

**Computational optimizations**: Compilation-level caching, no redundant work, binary search for position lookup

**Pragmatic trade-offs**: No parent pointers, no update methods, visible-string focus

### 8.3 Extensibility

**Generic abstractions**: Language-agnostic core (`TSyntaxKind`, `TSyntaxNode`)

**Pluggable detection**: Multiple strategies, language-specific detectors

**Feature contracts**: Uniform interfaces for classification, brace matching, completion, etc.

### 8.4 Correctness & Reliability

**Invariant enforcement**: 1:1 correspondence, contiguous coverage, well-formed input

**Error handling**: Parsers never throw, missing tokens synthesized, precise diagnostics

**Testing**: Match native parser behavior, verify span mapping, round-trip validation

### 8.5 User Experience Focus

**Parity with native parsers**: Error messages identical to runtime

**Feature quality**: Character-level precision, granular colorization, exact diagnostic spans

**Performance**: Instant response for visible strings, non-blocking, incremental (future)

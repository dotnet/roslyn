# Virtual Character System Specification

## 1. Overview & Motivation

### 1.1 The Problem

Embedded languages within string literals present a fundamental challenge: they need precise position tracking, but
string escaping collapses source text in non-trivial ways. IDE features like colorization, brace matching, completion,
and diagnostics require bidirectional mapping between logical characters and their source representations.

Without this mapping, features can only operate on the processed string value (`token.ValueText`), losing the connection
to what the user actually typed.

### 1.2 Examples Demonstrating the Need

Consider a normal string like `"Hello\tWorld"`. In source, the tab character appears as the two-character escape
sequence `\t`, but the logical string value contains an actual tab character. When the IDE needs to provide features
like completion or diagnostics, it must be able to map that single logical tab character back to its two-character
source representation at span [5, 7).

Verbatim strings present a different challenge. In `@"He said ""Hello"""`, the double-quote character in the logical
string `"He said "Hello""` comes from a doubled quote `""` in the source. Again, we need bidirectional mapping between
the single logical character and its two-character source representation.

Unicode escapes add yet another layer of complexity. In `"Test\u0041B"`, the six-character escape sequence `\u0041`
represents a single `A` character. The logical string is `"TestAB"`, but features must be able to map the `A` back to
its full six-character escape sequence in the source.

Even XML documentation uses character escaping. Inside a `<code>int x &lt; 5;</code>` block, the entity reference `&lt;`
(four characters) represents a single less-than character. When parsing C# code from documentation, we need to map that
logical `<` back to the `&lt;` entity reference.

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

The VirtualCharGreen is designed to be immutable and position-independent. Once created, it's never modified, and its
offset is stored relative to the token start rather than as an absolute file position. This makes instances shareable
across different contexts and enables efficient caching. The structure is also highly memory-optimized, packing both the
offset and width into a single integer field.

**Key fields**:
```csharp
internal readonly record struct VirtualCharGreen
{
    private const int MaxWidth = 12;
    private const int WidthMask = 0b1111;      // 4 bits for width (max 15)
    private const int OffsetShift = 4;         // remaining 28 bits for offset
    
    public readonly char Char;                  // The logical character
    private readonly int _offsetAndWidth;       // Packed offset + width
    
    public int Offset => _offsetAndWidth >> OffsetShift;
    public int Width => _offsetAndWidth & WidthMask;
    
    public VirtualCharGreen(char ch, int offset, int width)
    {
        Char = ch;
        _offsetAndWidth = (offset << OffsetShift) | width;
    }
}
```

**Packing details**:

The width field is limited to 4 bits, allowing values from 0 to 15. This is sufficient because the longest possible
escape sequence in C# is `\uXXXX\uXXXX` for a surrogate pair, which requires 12 characters. The remaining 28 bits are
used for the offset, which supports tokens up to 268 million characters long—far more than any realistic string literal.

**Examples**:
```csharp
// Regular character 'a' in "abc"
new VirtualCharGreen('a', offset: 1, width: 1)

// Tab character from "\t" escape
new VirtualCharGreen('\t', offset: 2, width: 2)

// 'A' from Unicode escape "\u0041"
new VirtualCharGreen('A', offset: 2, width: 6)
```

#### VirtualChar (Position-Aware)

**Red wrapper properties**:
```csharp
internal readonly record struct VirtualChar
{
    internal VirtualCharGreen Green { get; }
    internal int TokenStart { get; }
    
    public char Value => Green.Char;
    public TextSpan Span => new(TokenStart + Green.Offset, Green.Width);
    
    public static implicit operator char(VirtualChar ch) => ch.Value;
}
```

The VirtualChar structure wraps a VirtualCharGreen and adds a TokenStart field that provides absolute file position
context. The span is computed on-demand by combining the token's absolute start position with the green node's relative
offset and width. This wrapper is lightweight—just a green reference plus one integer—and supports implicit conversion
to char for convenient usage.

**Design rationale**: Following Roslyn's green/red split, the green component is immutable, shareable, and
position-independent (efficient for caching), while the red wrapper adds positional context on-demand (efficient for
consumption).

### 2.2 VirtualCharSequence & VirtualCharGreenSequence

#### Sequence Abstraction

Represents the complete processed contents of a string token as a sequence of VirtualChars.

**Structure**:
```csharp
internal partial struct VirtualCharGreenSequence
{
    private readonly Chunk _leafCharacters;    // The actual character storage
    private readonly TextSpan _span;           // Slice into _leafCharacters [inclusive, exclusive)
    
    public int Length => _span.Length;
    public VirtualCharGreen this[int index] => _leafCharacters[_span.Start + index];
    
    public VirtualCharGreenSequence Slice(int start, int length)
        => new(_leafCharacters, new TextSpan(_span.Start + start, length));
}

internal readonly struct VirtualCharSequence
{
    private readonly int _tokenStart;
    private readonly VirtualCharGreenSequence _sequence;
    
    public VirtualChar this[int index] => new(_sequence[index], _tokenStart);
    
    public VirtualCharSequence Slice(int start, int length)
        => new(_tokenStart, _sequence.Slice(start, length));
}
```

**Slicing support**: Efficient subsequence extraction without copying
- Example: `"\"Hello\""` → slice `[1..^1]` → `"Hello"` (no allocation)

#### Memory Optimization: Chunk Architecture

Two implementations optimized for different scenarios:

**StringChunk** (Common case: no escapes)
```csharp
// For tokens like "Hello World" (no escape sequences)
VirtualCharGreenSequence.Create("Hello World")

// Zero allocation: No array materialized
// Direct indexing: VirtualChar created on-demand from string
// Each character has width=1, offset matches string position
```

**ImmutableSegmentedListChunk** (Escapes present)
```csharp
// For tokens like "Hello\tWorld" (contains escapes)
var builder = ImmutableSegmentedList.CreateBuilder<VirtualCharGreen>();
builder.Add(new VirtualCharGreen('H', 0, 1));
builder.Add(new VirtualCharGreen('e', 1, 1));
builder.Add(new VirtualCharGreen('l', 2, 1));
builder.Add(new VirtualCharGreen('l', 3, 1));
builder.Add(new VirtualCharGreen('o', 4, 1));
builder.Add(new VirtualCharGreen('\t', 5, 2));  // \t spans 2 source chars
builder.Add(new VirtualCharGreen('W', 7, 1));
// ... etc
VirtualCharGreenSequence.Create(builder.ToImmutable())

// Materialized storage: Array holds pre-computed VirtualCharGreens
// Preserves escape info: Each element stores width for original escape sequence
```

**Performance impact**: 

For unescaped strings (the common case), there's no heap allocation beyond the string token itself. When escapes are
present, we allocate a single array to hold the materialized VirtualCharGreens. Both implementations support efficient
slicing without copying the underlying data.

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

The service is responsible for validating that the token is a well-formed string literal without diagnostics, processing
the language-specific escape sequences, generating the VirtualChar sequence while maintaining all invariants, and
providing reverse mapping from logical characters back to their escape form.

**Implementations**:
- `CSharpVirtualCharService`: Handles C# string escaping rules
- `VisualBasicVirtualCharService`: Handles VB string escaping rules

## 3. Embedded Language Detection

The system must identify which string literals contain embedded languages and which specific language they contain.

### 3.1 Detection Strategies

#### Strategy 1: `[StringSyntax]` Attribute

The most explicit detection mechanism uses .NET 7+ `System.Diagnostics.CodeAnalysis.StringSyntaxAttribute`.

**Example**: `void ProcessRegex([StringSyntax("Regex")] string pattern)`

The detector checks for this attribute in several locations: method and constructor parameters, field declarations,
property declarations, and even attribute constructor arguments. The algorithm parses the argument syntax, resolves the
parameter symbol via the semantic model, checks for the StringSyntax attribute, and extracts the language identifier
from the first constructor argument.

#### Strategy 2: Comment Annotations

Lightweight annotation using special comment syntax: `// lang=<identifier>[,<option1>,<option2>,...]`

**Example**:
```csharp
// lang=regex
var pattern = "\\d+";
```

The comment applies to the next statement or declaration. The detector scans both the leading trivia of the statement
and the trailing trivia of the previous token to find these annotations. Comments can include comma-separated options
that are passed to the parser configuration. Importantly, comments take precedence over attribute detection, allowing
developers to override the default detection when needed.

#### Strategy 3: Well-Known APIs

Recognition of framework types and methods known to accept embedded language strings.

**Regex APIs**: `Regex.IsMatch`, `Regex.Replace`, `new Regex(...)`, etc.

The recognition logic maintains a hash set of well-known method names, verifies that the invoked symbol belongs to the
expected type (like `System.Text.RegularExpressions.Regex`), and then finds parameters with specific names (like
`"pattern"`) to match against arguments. This API registry is built once at the compilation level from the type's
members.

#### Strategy 4: Interpolation Format String Analysis

Special handling for format strings in interpolated string expressions.

**Example**: `$"{date:yyyy-MM-dd}"`

**Detection flow**:
1. Identify format portion (`:yyyy-MM-dd`)
2. Get type of expression (`DateTime`)
3. Find `IFormattable.ToString` implementation
4. Check first parameter for `[StringSyntax]` attribute

### 3.2 Language Detector Architecture

Detectors are compilation-scoped services that efficiently identify embedded language tokens. They cache type symbols
(like the `Regex` type) so they only need to be resolved once, and similarly cache the set of well-known method names
computed from the type's members. The detectors don't cache parsed trees, however—those are built on-demand only for
tokens that are currently visible in the editor.

The system uses a generic `EmbeddedLanguageDetector` infrastructure that works with language identifiers and the various
detection strategies described above.

### 3.3 Detection Flow Examples

For direct parameter annotation, the flow is straightforward: a string literal appears as an argument, which maps to a
parameter decorated with `[StringSyntax]`, and detection succeeds immediately.

Local variable flow tracking is more complex. When a string literal is assigned to a local variable, no detection occurs
initially. Later, when that variable is passed to a well-known API like `Regex.IsMatch`, the detector backtracks from
the usage site to the original assignment and marks the string literal as containing an embedded language.

For fields with const or readonly modifiers, the detector finds the attribute on the field declaration, then scans the
containing type for all references to that field and marks those usage sites as well.

Comment overrides take precedence over all other strategies, allowing developers to explicitly specify the language when
the automatic detection might be incorrect.

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
```csharp
internal readonly struct EmbeddedSyntaxToken<TSyntaxKind>
{
    public readonly TSyntaxKind Kind;
    public readonly ImmutableArray<EmbeddedSyntaxTrivia<TSyntaxKind>> LeadingTrivia;
    public readonly VirtualCharSequence VirtualChars;
    public readonly ImmutableArray<EmbeddedSyntaxTrivia<TSyntaxKind>> TrailingTrivia;
    internal readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;
    public readonly object Value;  // Optional semantic interpretation
    
    public bool IsMissing => VirtualChars.IsEmpty();
    
    public TextSpan GetSpan() 
        => VirtualChars.Length == 0 
            ? default 
            : TextSpan.FromBounds(
                VirtualChars[0].Span.Start, 
                VirtualChars[^1].Span.End);
}
```

**Trivia handling**: Limited compared to Roslyn
- Regex: Whitespace/comments only in `IgnorePatternWhitespace` mode
- JSON: Whitespace, single-line (`//`), multi-line (`/* */`) comments

**Position**: Derived from VirtualChars (first char start to last char end)

**Value examples**:
- Number tokens: parsed `int` or `double`
- Regex capture tokens: capture name as `string`
- Missing tokens: `null`

### 4.3 EmbeddedSyntaxNode

Abstract base for all non-terminal nodes in embedded syntax trees.

**Key characteristics**:
```csharp
internal abstract class EmbeddedSyntaxNode<TSyntaxKind, TSyntaxNode>
{
    public readonly TSyntaxKind Kind;
    
    internal abstract int ChildCount { get; }
    internal abstract EmbeddedSyntaxNodeOrToken<TSyntaxKind, TSyntaxNode> ChildAt(int index);
    
    public TextSpan GetSpan()
    {
        var start = int.MaxValue;
        var end = 0;
        
        foreach (var child in this)
        {
            if (child.IsNode)
                child.Node.GetSpan(ref start, ref end);
            else if (!child.Token.IsMissing)
            {
                start = Math.Min(child.Token.VirtualChars[0].Span.Start, start);
                end = Math.Max(child.Token.VirtualChars[^1].Span.End, end);
            }
        }
        
        return TextSpan.FromBounds(start, end);
    }
    
    public bool Contains(VirtualChar virtualChar)
    {
        foreach (var child in this)
        {
            if (child.IsNode)
            {
                if (child.Node.Contains(virtualChar))
                    return true;
            }
            else if (child.Token.VirtualChars.Contains(virtualChar))
            {
                return true;
            }
        }
        return false;
    }
}
```

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

The root tree structure contains three main components. The `VirtualCharSequence Text` field is the source of truth for
all position information. The `TCompilationUnitSyntax Root` is the top-level syntax node, which is always present (never
null) and contains the entire parsed structure. Finally, `ImmutableArray<EmbeddedDiagnostic> Diagnostics` holds all
errors and warnings found during parsing, deduplicated so that the same diagnostic doesn't appear twice at the same
position.

Concrete instantiations of this structure include `RegexTree`, which adds language-specific dictionaries for capture
names and numbers, and `JsonTree`, which is a pure tree without additional properties.

### 4.6 Parsing Pipeline

The parsing pipeline flows from a source `SyntaxToken` through `IVirtualCharService` to produce a `VirtualCharSequence`,
which is then consumed by `Parser.TryParse()` to yield an `EmbeddedSyntaxTree`.

Parsers are designed to always succeed except in the rare case of stack overflow. They maintain full fidelity by
representing every VirtualChar in the resulting tree. When errors are encountered, the parser synthesizes missing tokens
and attaches diagnostics rather than failing. An important characteristic is that diagnostics precisely replicate the
error messages that native parsers would produce, ensuring consistency with runtime behavior.

#### Example: JSON Lexer & Parser (Simplified)

**Lexer structure**:
```csharp
internal struct JsonLexer
{
    public readonly VirtualCharSequence Text;
    public int Position;
    
    public JsonToken ScanNextToken()
    {
        var leadingTrivia = ScanTrivia(leading: true);
        
        if (Position == Text.Length)
            return CreateToken(JsonKind.EndOfFile, leadingTrivia, 
                VirtualCharSequence.Empty, []);
        
        var (chars, kind, diagnostic) = ScanNextTokenWorker();
        var trailingTrivia = ScanTrivia(leading: false);
        var token = CreateToken(kind, leadingTrivia, chars, trailingTrivia);
        
        return diagnostic == null 
            ? token 
            : token.AddDiagnosticIfNone(diagnostic.Value);
    }
    
    private (VirtualCharSequence, JsonKind, EmbeddedDiagnostic?) ScanNextTokenWorker()
    {
        return this.CurrentChar.Value switch
        {
            '{' => ScanSingleCharToken(JsonKind.OpenBraceToken),
            '}' => ScanSingleCharToken(JsonKind.CloseBraceToken),
            '[' => ScanSingleCharToken(JsonKind.OpenBracketToken),
            ']' => ScanSingleCharToken(JsonKind.CloseBracketToken),
            ',' => ScanSingleCharToken(JsonKind.CommaToken),
            ':' => ScanSingleCharToken(JsonKind.ColonToken),
            '\'' or '"' => ScanString(),
            _ => ScanText(),
        };
    }
    
    private (VirtualCharSequence, JsonKind, EmbeddedDiagnostic?) ScanString()
    {
        var start = Position;
        var openChar = this.CurrentChar;
        Position++;
        
        while (Position < Text.Length)
        {
            var ch = this.CurrentChar;
            Position++;
            
            if (ch.Value == openChar.Value)
                return (GetCharsToCurrentPosition(start), JsonKind.StringToken, null);
            
            if (ch.Value == '\\')
                AdvanceToEndOfEscape();
        }
        
        // Unterminated string
        var chars = GetCharsToCurrentPosition(start);
        return (chars, JsonKind.StringToken, 
            new EmbeddedDiagnostic("Unterminated string", GetSpan(chars)));
    }
}
```

**Parser structure**:
```csharp
internal partial struct JsonParser
{
    private JsonLexer _lexer;
    private JsonToken _currentToken;
    
    public static JsonTree? TryParse(VirtualCharSequence text, JsonOptions options)
    {
        try
        {
            if (text.IsDefaultOrEmpty())
                return null;
            
            return new JsonParser(text).ParseTree(options);
        }
        catch (InsufficientExecutionStackException)
        {
            return null;
        }
    }
    
    private JsonTree ParseTree(JsonOptions options)
    {
        var sequence = this.ParseSequence();
        var root = new JsonCompilationUnit(sequence, _currentToken);
        
        // Collect diagnostics from tree and run validation passes
        var diagnostics = GetDiagnostics(root, options);
        
        return new JsonTree(_lexer.Text, root, diagnostics);
    }
    
    private ImmutableArray<JsonValueNode> ParseSequence()
    {
        var result = ArrayBuilder<JsonValueNode>.GetInstance();
        
        while (ShouldConsumeSequenceElement())
            result.Add(ParseValue());
        
        return result.ToImmutableAndFree();
    }
    
    private JsonValueNode ParseValue()
    {
        return _currentToken.Kind switch
        {
            JsonKind.OpenBraceToken => ParseObject(),
            JsonKind.OpenBracketToken => ParseArray(),
            _ => ParseLiteral(),
        };
    }
    
    private JsonObjectNode ParseObject()
    {
        var openBrace = ConsumeCurrentToken();
        var properties = ParseCommaSeparatedSequence();
        var closeBrace = ConsumeToken(JsonKind.CloseBraceToken, "'}' expected");
        
        return new JsonObjectNode(openBrace, properties, closeBrace);
    }
}
```

The key patterns in this architecture are straightforward. The lexer consumes VirtualChars and produces tokens that
carry VirtualChar spans for precise position tracking. The parser then consumes these tokens to build the tree
structure. Diagnostics are attached during parsing as errors are encountered, then aggregated into the final tree. When
required tokens are missing, the parser synthesizes them with attached diagnostics to enable error recovery while
maintaining a complete tree structure.

## 5. Feature Integration

### 5.1 Brace Matching

Highlights matching delimiters when cursor is adjacent.

The algorithm works in several steps. First, it converts the cursor position to a VirtualChar using
`tree.Text.Find(position)`. Then it walks the tree to find the node containing that character through recursive descent.
Once found, it extracts the open and close tokens from the grouping, character class, or other bracketed node, and
returns the span pair for highlighting.

**Example implementation (JSON)**:
```csharp
internal sealed class JsonBraceMatcher : IEmbeddedLanguageBraceMatcher
{
    public BraceMatchingResult? FindBraces(
        SemanticModel semanticModel,
        SyntaxToken token,
        int position,
        CancellationToken cancellationToken)
    {
        var tree = ParseJsonTree(token, semanticModel, cancellationToken);
        if (tree == null)
            return null;
        
        // Step 1: Find VirtualChar at cursor position
        var virtualChar = tree.Text.Find(position);
        if (virtualChar == null)
            return null;
        
        var ch = virtualChar.Value;
        
        // Step 2: Only process brace-like characters
        if (ch.Value is not ('{' or '[' or '(' or '}' or ']' or ')'))
            return null;
        
        // Step 3: Find the node containing this character
        return FindBraceMatchingResult(tree.Root, ch);
    }
    
    private static BraceMatchingResult? FindBraceMatchingResult(
        JsonNode node, VirtualChar ch)
    {
        // Check if this node's span contains the character
        var fullSpan = node.GetFullSpan();
        if (fullSpan == null || !fullSpan.Value.Contains(ch.Span.Start))
            return null;
        
        // Check if this node is a matching construct
        switch (node)
        {
            case JsonArrayNode array 
                when Matches(array.OpenBracketToken, array.CloseBracketToken, ch):
                return Create(array.OpenBracketToken, array.CloseBracketToken);
            
            case JsonObjectNode obj 
                when Matches(obj.OpenBraceToken, obj.CloseBraceToken, ch):
                return Create(obj.OpenBraceToken, obj.CloseBraceToken);
            
            case JsonConstructorNode cons 
                when Matches(cons.OpenParenToken, cons.CloseParenToken, ch):
                return Create(cons.OpenParenToken, cons.CloseParenToken);
        }
        
        // Recursively search children
        foreach (var child in node)
        {
            if (child.IsNode)
            {
                var result = FindBraceMatchingResult(child.Node, ch);
                if (result != null)
                    return result;
            }
        }
        
        return null;
    }
    
    private static BraceMatchingResult? Create(JsonToken open, JsonToken close)
        => open.IsMissing || close.IsMissing
            ? null
            : new BraceMatchingResult(open.GetSpan(), close.GetSpan());
    
    private static bool Matches(JsonToken openToken, JsonToken closeToken, VirtualChar ch)
        => openToken.VirtualChars.Contains(ch) || closeToken.VirtualChars.Contains(ch);
}
```

**Supported constructs**: Parentheses, brackets, braces, comment delimiters

### 5.2 Classification & Colorization

Provides syntax highlighting within embedded language strings.

The classification process walks all tokens in the embedded syntax tree and maps each token's kind to a classification
type (for example, `RegexKind.NumberToken` maps to `"regex - quantifier"`). The VirtualChar spans from each token are
extracted to produce source TextSpans, which are then published to the IDE for colorization. This approach provides
granular coloring where individual constructs like escape sequences, keywords, and operators each get distinct
highlighting.

### 5.3 Diagnostics

Errors and warnings reported with precise source spans.

Diagnostics are attached during parsing to tokens and trivia as they're created, then aggregated into the final tree.
The aggregation process ensures deduplication so that the same diagnostic doesn't appear multiple times at the same
position. These diagnostics integrate seamlessly with the IDE because the VirtualChar spans map directly to the
locations where squiggles should appear.

**Example**:
```csharp
// During parsing:
var token = ScanString();
if (position == text.Length)  // Unterminated string
{
    token = token.AddDiagnosticIfNone(new EmbeddedDiagnostic(
        "Unterminated string",
        token.GetSpan()));
}

// Tree aggregation:
var allDiagnostics = CollectDiagnostics(tree.Root);
return new JsonTree(text, root, allDiagnostics);
```

### 5.4 Completion

Context-sensitive suggestions within embedded language strings.

**Trigger scenarios**:
- After `\` in regex: offer escape sequences
- After `\k<` or `\<`: offer capture names from tree
- Inside JSON: offer property names, keywords (future)

**Example**:
```csharp
var virtualChar = tree.Text.Find(position);
if (virtualChar == null)
    return null;

// Check if we're after a backslash
if (virtualChar.Value == '\\')
{
    // Offer escape sequences
    yield return new CompletionItem("\\d", "digit character");
    yield return new CompletionItem("\\w", "word character");
    yield return new CompletionItem("\\s", "whitespace character");
    // etc.
}
```

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

**Example tree structure**:
```csharp
// Input: {"key": 123}
JsonCompilationUnit
├─ Sequence
│  └─ JsonObjectNode
│     ├─ OpenBraceToken: '{'
│     ├─ Sequence (separated list)
│     │  ├─ JsonPropertyNode
│     │  │  ├─ NameToken: "key"
│     │  │  ├─ ColonToken: ':'
│     │  │  └─ Value: JsonLiteralNode
│     │  │     └─ LiteralToken: 123 (NumberToken)
│     └─ CloseBraceToken: '}'
└─ EndOfFileToken
```

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

The system enforces critical invariants including 1:1 character correspondence, contiguous span coverage, and
well-formed input requirements. Error handling is designed to never throw exceptions—instead, parsers synthesize missing
tokens and attach precise diagnostics. The testing strategy focuses on matching native parser behavior, verifying span
mappings, and validating round-trip conversion.

### 8.5 User Experience Focus

The system aims for complete parity with native parsers, ensuring that error messages match exactly what the runtime
would produce. Feature quality is measured by character-level precision, granular colorization, and exact diagnostic
spans. From a performance perspective, features respond instantly for visible strings, run non-blocking on background
threads, and are designed for incremental updates (planned for future).

# Roslyn Highlighting Taggers: Deep Dive Analysis

## Overview

Roslyn's highlighting taggers provide interactive visual feedback in the editor. This analysis explores three key taggers: reference highlighting, brace matching, and line separators, examining their architecture, algorithms, and design decisions.

---

## 1. Reference Highlighting (`ReferenceHighlightingViewTaggerProvider`)

### What It Does
When you click on a symbol (variable, method, class, etc.), all references to that symbol are highlighted throughout the visible documents.

### Architecture

**Base Class**: `AsynchronousViewTaggerProvider<NavigableHighlightTag>`
- Inherits from `AbstractAsynchronousTaggerProvider`
- Implements `IViewTaggerProvider` (not `ITaggerProvider`)

**Why `IViewTaggerProvider`?**
Reference highlighting is **view-specific** because:
1. It depends on **caret position** - different views can have different caret positions
2. Each view needs independent highlighting state
3. The tagger needs access to `ITextView` to determine where the caret is

```csharp
protected override SnapshotPoint? GetCaretPoint(ITextView textViewOpt, ITextBuffer subjectBuffer)
{
    // With no selection we just use the caret position as expected
    if (textViewOpt.Selection.IsEmpty)
    {
        return textViewOpt.Caret.Position.Point.GetPoint(...);
    }
    // If there is a selection then it makes more sense for highlighting to apply 
    // to the token at the start of the selection...
}
```

### Triggering Mechanism

**Event Sources** (lines 57-65):
```csharp
protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
{
    return TaggerEventSources.Compose(
        TaggerEventSources.OnCaretPositionChanged(textView, textView.TextBuffer),
        TaggerEventSources.OnWorkspaceChanged(subjectBuffer, AsyncListener),
        TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer));
}
```

- **Primary trigger**: Caret position changes
- **Secondary triggers**: Workspace changes (files added/removed), document context changes
- **Note**: Does NOT listen to `OnTextChanged` directly - text changes are reported via `OnSemanticChanged`

### Finding References

**Algorithm** (lines 136-171):
1. Gets the symbol at the caret position using `SymbolFinder.FindSymbolAtPositionAsync`
2. Calls `IDocumentHighlightsService.GetDocumentHighlightsAsync` which:
   - Uses `SymbolFinder.FindReferencesInDocumentsInCurrentProcessAsync` to find all references
   - Filters to only documents that correspond to visible snapshots
   - Returns `DocumentHighlights` with `HighlightSpan` objects

**Key Service**: `AbstractDocumentHighlightsService`
- Can run in-process or out-of-process (OOP) via `RemoteDocumentHighlightsService`
- Uses nullable-disabled semantic model for performance (line 68)
- Supports embedded languages (regex, JSON) via `IEmbeddedLanguageDocumentHighlighter`

### Performance Optimizations

1. **Smart Reuse** (lines 120-129):
   ```csharp
   // See if the user is just moving their caret around in an existing tag
   var onExistingTags = context.HasExistingContainingTags(caretPosition);
   if (onExistingTags)
   {
       context.SetSpansTagged([]);
       return; // Don't recompute!
   }
   ```
   If the caret moves within an existing highlighted span, don't recompute.

2. **Frozen Partial Semantics** (line 55):
   ```csharp
   protected override bool SupportsFrozenPartialSemantics => true;
   ```
   - First pass: Quick results using frozen documents (no waiting for SG docs)
   - Second pass: Accurate results using full semantic model
   - Second pass is aggressively canceled when new work arrives

3. **Delay Strategy**:
   - `TaggerDelay.Medium` (line 49) - balances responsiveness with batching

### Tag Types

Three types of highlight tags:
- `WrittenReferenceHighlightTag` - write references (assignments)
- `DefinitionHighlightTag` - definition sites
- `ReferenceHighlightTag` - read references

### Behavior on Text Changes

```csharp
protected override TaggerCaretChangeBehavior CaretChangeBehavior 
    => TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag;
protected override TaggerTextChangeBehavior TextChangeBehavior 
    => TaggerTextChangeBehavior.RemoveAllTags;
```

- **Text changes**: Clear all highlights immediately
- **Caret moves**: Preserve highlights if caret stays within a tag, otherwise clear

---

## 2. Brace Matching (`BraceHighlightingViewTaggerProvider`)

### What It Does
When your cursor is on a brace character (`{`, `}`, `(`, `)`, `[`, `]`, etc.), the matching brace is highlighted. Works for nested braces and even non-brace pairs like `#region`/`#endregion`.

### Architecture

**Base Class**: `AsynchronousViewTaggerProvider<BraceHighlightTag>`
- Also uses `IViewTaggerProvider` because it depends on caret position

**Delay**: `TaggerDelay.NearImmediate` (line 36) - must feel instant!

### Algorithm

**The Core Insight** (lines 77-91):
The tagger checks **two positions**:
1. **Position** (right of caret) - finds braces like `^[ ]` or `[^ ]`
2. **Position - 1** (left of caret) - finds braces like `[ ]^` or `[ ]^`

**Implementation** (`GetAllMatchingBracesAsync`, lines 92-148):

```csharp
// Check position (right of caret)
var rightOfPosition = await service.GetMatchingBracesAsync(document, position, ...);

// Only add if position is actually within the start brace span
if (rightOfPosition.HasValue && 
    !rightOfPosition.Value.LeftSpan.Contains(position))
{
    rightOfPosition = null; // Not valid
}

// Check position - 1 (left of caret)
if (position > 0)
{
    var leftOfPosition = await service.GetMatchingBracesAsync(document, position - 1, ...);
    
    // Only add if position is within the end brace span
    if (leftOfPosition.HasValue &&
        position <= leftOfPosition.Value.RightSpan.End &&
        position > leftOfPosition.Value.RightSpan.Start)
    {
        return (leftOfPosition, rightOfPosition);
    }
}
```

**Examples**:
- `()^()` - Returns both pairs (caret between two complete pairs)
- `(^())` - Returns only inner `()` (caret is inside outer, so outer not highlighted)
- `^{ }` - Returns `{ }` (caret at start of opening brace)
- `{ }^` - Returns `{ }` (caret at end of closing brace)
- `{^ }` - Returns nothing (caret is inside, not at boundary)

### Finding Matching Braces

**Service**: `IBraceMatchingService` → `BraceMatchingService`
- Delegates to language-specific `IBraceMatcher` implementations
- Multiple matchers can exist (e.g., C# braces, regex braces, JSON braces)

**Base Implementation**: `AbstractBraceMatcher.FindBracesAsync` (lines 58-89)

```csharp
var root = await document.GetSyntaxRootAsync(cancellationToken);
var token = root.FindToken(position);

if (position < text.Length && this.IsBrace(text[position]))
{
    if (token.RawKind == _openBrace.Kind && AllowedForToken(token))
    {
        var leftToken = token;
        if (TryFindMatchingToken(leftToken, out var rightToken))
        {
            return new BraceMatchingResult(leftToken.Span, rightToken.Span);
        }
    }
    // Similar for close brace...
}
```

**Matching Algorithm** (`TryFindMatchingToken`, lines 27-56):
1. Gets the parent node of the token
2. Finds all brace tokens in the parent's children
3. If exactly 2 tokens found (open + close), checks if current token matches
4. Returns the matching token

**Why This Works for Nested Braces**:
- The syntax tree structure naturally handles nesting
- `FindToken(position)` returns the **innermost** token at that position
- The parent node contains the immediate matching pair
- Nested braces are in different parent nodes, so they're handled separately

**Example**:
```
{  // Parent: BlockSyntax
    if (x) { }  // Parent: IfStatementSyntax
}
```
- At `{` of outer: Parent is `BlockSyntax`, finds matching `}`
- At `{` of inner: Parent is `IfStatementSyntax`, finds matching `}`

### Non-Brace Pairs: Directives

**Special Matcher**: `AbstractDirectiveTriviaBraceMatcher`
- Handles `#region`/`#endregion` and `#if`/`#elif`/`#else`/`#endif`
- Uses syntax tree to find matching directives
- For `#region`: Uses `GetMatchingDirective` which walks the syntax tree
- For `#if` chains: Uses `GetMatchingConditionalDirectives` which returns the chain

**Implementation** (lines 29-66):
```csharp
var token = root.FindToken(position, findInsideTrivia: true);
if (token.Parent is not TDirectiveTriviaSyntax directive)
    return null;

if (directive is TRegionDirectiveTriviaSyntax or TEndRegionDirectiveTriviaSyntax)
{
    matchingDirective = GetMatchingDirective(directive, cancellationToken);
}
else if (directive is TIfDirectiveTriviaSyntax or ...)
{
    var matchingDirectives = GetMatchingConditionalDirectives(directive, cancellationToken);
    // Returns next directive in chain
    matchingDirective = matchingDirectives[(matchingDirectives.IndexOf(directive) + 1) % matchingDirectives.Length];
}
```

### Multi-Character Braces

The algorithm handles multi-character braces like `<@` and `@>`:
- Position must be on the **left side** of start brace, or **inside** start brace (but not at end)
- Examples:
  - `^<@ @>` - Returns match (position at start)
  - `<^@ @>` - Returns match (position inside start)
  - `<@^ @>` - No match (position at end of start)

---

## 3. Line Separators (`LineSeparatorTaggerProvider`)

### What It Does
Shows faint horizontal lines between methods, classes, and other top-level constructs to visually separate code blocks.

### Architecture

**Base Class**: `AsynchronousTaggerProvider<LineSeparatorTag>`
- Uses `ITaggerProvider` (NOT `IViewTaggerProvider`) because:
  - **Doesn't depend on caret position**
  - **Doesn't depend on view state**
  - Same separators should appear in all views of the same document

**Key Difference**:
- `IViewTaggerProvider`: Creates taggers per view → stored in `textView.TryGetPerSubjectBufferProperty`
- `ITaggerProvider`: Creates taggers per buffer → stored in `subjectBuffer.Properties`

### Implementation

**Service**: `ILineSeparatorService` (language-specific)
- C#: `CSharpLineSeparatorService`
- VB: `VisualBasicLineSeparatorService`

**Algorithm** (from `CSharpLineSeparatorService`):
1. Walks the syntax tree
2. Identifies "separable blocks" (methods, properties, classes, etc.)
3. Adds line separators:
   - After each separable block (except the last)
   - Before the first separable block if preceded by non-separable content
   - After the last separable block if it's at the top level

**Example Logic** (pseudocode):
```csharp
foreach (child in children)
{
    if (IsSeparableBlock(child))
    {
        if (!seenSeparator && previousChild != null)
        {
            AddSeparatorAfter(previousChild);
        }
        AddSeparatorAfter(child);
        seenSeparator = true;
    }
    else
    {
        seenSeparator = false;
    }
}
```

**Delay**: `TaggerDelay.NearImmediate` - should update quickly as code changes

---

## 4. View-Specific vs Document-Level State

### How Multiple Views Are Handled

**View-Specific Taggers** (`IViewTaggerProvider`):
```csharp
// From AbstractAsynchronousTaggerProvider.cs lines 169-198
private bool TryRetrieveTagSource(ITextView? textView, ITextBuffer subjectBuffer, ...)
{
    return textView != null
        ? textView.TryGetPerSubjectBufferProperty(subjectBuffer, _uniqueKey, out tagSource)
        : subjectBuffer.Properties.TryGetProperty(_uniqueKey, out tagSource);
}
```

- Each `ITextView` has its own `TagSource` instance
- Stored via `textView.AddPerSubjectBufferProperty`
- Allows different views to have different:
  - Caret positions
  - Highlighting states
  - Tag computation schedules

**Document-Level Taggers** (`ITaggerProvider`):
- Single `TagSource` per `ITextBuffer`
- Stored via `subjectBuffer.Properties.AddProperty`
- All views share the same tags
- More efficient (compute once, display everywhere)

### Example: Reference Highlighting

If you have **two views** of the same file:
- **View 1**: Caret on `MyMethod` → highlights all `MyMethod` references
- **View 2**: Caret on `MyClass` → highlights all `MyClass` references
- Each view maintains independent highlighting state
- Each view's tagger runs independently

### Example: Line Separators

If you have **two views** of the same file:
- Both views show the **same** line separators
- Only one `TagSource` computes the separators
- Both views consume the same tags
- More efficient!

---

## 5. Cancellation and Responsiveness

### Delay Strategies

**TaggerDelay Enum** (from `TaggerDelay.cs`):
- `NearImmediate`: For features that must feel instant (brace matching, line separators)
- `Short`: User typing quickly shouldn't trigger, but pauses will
- `Medium`: More significant pause needed (reference highlighting)
- `OnIdle`: Run when user appears idle
- `OnIdleWithLongDelay`: Very expensive operations (10s+ delay)
- `NonFocus`: For non-visible content

**Usage**:
```csharp
// Reference highlighting - can wait a bit
protected override TaggerDelay EventChangeDelay => TaggerDelay.Medium;

// Brace matching - must be instant!
protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;
```

### Cancellation Mechanisms

**1. Event-Based Cancellation**:
- When new events arrive, previous tag computation can be canceled
- Controlled by `CancelOnNewWork` property (default: true for most taggers)

**2. Token Propagation**:
```csharp
// From AbstractAsynchronousTaggerProvider.TagSource_ProduceTags.cs
using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
    lastNonFrozenComputationToken, 
    cancellationToken);
await RecomputeTagsAsync(..., linkedTokenSource.Token);
```

**3. Aggressive Cancellation for Expensive Work**:
- Frozen partial semantics: Second pass (expensive) is aggressively canceled
- Non-visible content: Tagging delayed until visible
- Multiple cancellation tokens linked together

**4. Checkpoints**:
```csharp
// Throughout ProduceTagsAsync implementations:
cancellationToken.ThrowIfCancellationRequested();
// ... do work ...
cancellationToken.ThrowIfCancellationRequested();
```

### Responsiveness Strategies

**1. Frozen Partial Semantics** (Reference Highlighting):
```csharp
protected override bool SupportsFrozenPartialSemantics => true;
```
- **First pass**: Quick results using frozen documents (no SG docs)
- **Second pass**: Accurate results (can be canceled if new work arrives)
- User sees highlights quickly, then they refine

**2. Smart Tag Reuse**:
- Reference highlighting: If caret moves within existing tags, don't recompute
- Brace matching: Very fast (syntax tree lookup), so always recomputes

**3. Batching**:
- Events are batched together
- Tag computation happens after delay period
- Reduces redundant work

**4. Visibility Tracking**:
```csharp
// From AbstractAsynchronousTaggerProvider.TagSource_ProduceTags.cs
if (!highPriority && !isVisible)
{
    await _dataSource.VisibilityTracker.DelayWhileNonVisibleAsync(...);
}
```
- Non-visible content tagged with lower priority
- Saves resources when editor is minimized or tab not visible

---

## Key Design Patterns

### 1. Separation of Concerns
- **Tagger Provider**: Handles editor integration, event sources, delays
- **Language Service**: Handles language-specific logic (finding references, braces, etc.)
- **Tag**: Represents the visual marker (highlight, brace, separator)

### 2. Async Everywhere
- All tag computation is async
- Uses `ConfigureAwait(false)` to avoid UI thread blocking
- Cancellation tokens propagated throughout

### 3. Event-Driven Architecture
- `ITaggerEventSource` notifies of changes
- Taggers react to events, not polling
- Composable event sources (`TaggerEventSources.Compose`)

### 4. Efficient Tag Comparison
- `TagEquals` method for diffing tags
- Prevents unnecessary UI updates
- Many tags are singletons (reference equality)

### 5. Span Tracking
- `SpanTrackingMode` controls how tags adapt to text changes
- `EdgeExclusive` is common (tags track by edges, not content)

---

## Summary

### Reference Highlighting
- **Why view-specific**: Depends on caret position
- **How references found**: `SymbolFinder.FindReferencesInDocumentsInCurrentProcessAsync`
- **Performance**: Frozen partial semantics, smart reuse, medium delay
- **Trigger**: Caret position changes, workspace changes

### Brace Matching
- **Why view-specific**: Depends on caret position
- **Algorithm**: Check position and position-1, use syntax tree to find matching tokens
- **Nested braces**: Handled naturally by syntax tree structure
- **Directives**: Special matcher for `#region`/`#endregion` and `#if` chains
- **Performance**: Near-immediate delay, very fast syntax tree lookup

### Line Separators
- **Why document-level**: Doesn't depend on view state
- **Algorithm**: Syntax tree walk to find separable blocks
- **Performance**: Near-immediate delay, computed once per buffer

### Multiple Views
- View-specific taggers: One `TagSource` per view
- Document-level taggers: One `TagSource` per buffer
- Stored via `textView.TryGetPerSubjectBufferProperty` vs `subjectBuffer.Properties`

### Cancellation & Responsiveness
- `TaggerDelay` enum controls update frequency
- Cancellation tokens propagated through async operations
- Aggressive cancellation of expensive work
- Frozen partial semantics for quick initial results
- Visibility tracking to prioritize visible content

---

## Conclusion

Roslyn's highlighting taggers demonstrate sophisticated engineering:
- **Separation**: Clear boundaries between editor integration and language logic
- **Performance**: Multiple optimization strategies (frozen semantics, smart reuse, batching)
- **Responsiveness**: Near-immediate feedback where needed, appropriate delays elsewhere
- **Scalability**: Efficient handling of multiple views, cancellation, and resource management
- **Extensibility**: Language services allow different languages to provide their own implementations

The choice between `IViewTaggerProvider` and `ITaggerProvider` is fundamental: use view-specific when you need caret position or view state, use document-level when tags are independent of the view.

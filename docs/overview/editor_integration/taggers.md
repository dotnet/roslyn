# Taggers: Deep Dive

| | |
|---|---|
| **Last Updated** | January 29, 2026 |
| **Parent Doc** | [Editor Integration Overview](./codebase_overview.md) |

---

## Why Taggers Exist

Visual Studio's editor separates text storage (`ITextBuffer`) from visual presentation (`ITextView`). The editor renders text, but it doesn't understand C# or VB—it just sees characters.

Roslyn understands the code deeply: syntax structure, semantic meaning, diagnostics, symbol references. But Roslyn's model is separate from the editor's model.

**Taggers bridge this gap.** They translate Roslyn's understanding into the editor's visual language:

- Roslyn says "this span is a keyword" → Tagger provides a classification tag → Editor colors it blue
- Roslyn says "this span has an error" → Tagger provides an error tag → Editor draws a red squiggle
- Roslyn says "these spans reference the same symbol" → Tagger provides highlight tags → Editor highlights them

Without taggers, the editor would show plain monochrome text with no errors, no highlighting, no visual feedback.

---

## Core Abstractions

### Tags and Tag Spans

A **tag** is metadata attached to a text span. Different tag types trigger different editor behaviors:

| Tag Type | Editor Behavior |
|----------|-----------------|
| `IClassificationTag` | Syntax coloring |
| `IErrorTag` | Error/warning squiggles |
| `ITextMarkerTag` | Background highlighting |
| `IOutliningRegionTag` | Code folding regions |
| `IStructureTag` | Structural guides |

A `TagSpan<T>` pairs a tag with its location in the text.

### Tagger Providers

The editor doesn't create taggers directly—it asks **providers** to create them. There are two provider interfaces:

**`ITaggerProvider`** — Creates taggers scoped to a text buffer.
- One tagger instance shared across all views of the same buffer
- Use when tags don't depend on view state (caret position, viewport, selection)
- Examples: syntax classification, diagnostics, code structure

**`IViewTaggerProvider`** — Creates taggers scoped to a specific view.
- Separate tagger instance per view
- Use when tags depend on view state
- Examples: reference highlighting (depends on caret position), brace matching

The distinction matters because multiple views can show the same buffer (split windows, peek definition). Buffer-scoped taggers compute once and share; view-scoped taggers compute independently per view.

### Registration

Providers register via MEF with attributes specifying when they apply:

```csharp
[Export(typeof(ITaggerProvider))]
[TagType(typeof(IClassificationTag))]
[ContentType(ContentTypeNames.RoslynContentType)]
internal class MyTaggerProvider : ITaggerProvider { }
```

- `TagType` — What kind of tags this provider produces
- `ContentType` — What file types this provider handles

---

## The Async Pattern

### Why Async?

Computing tags often requires semantic information: parsing, binding, analysis. This work can take hundreds of milliseconds—far too slow for the UI thread.

Synchronous tagging would mean:
- Typing lag (UI blocks while computing tags)
- Scroll lag (recomputing visible tags blocks rendering)
- Compounding delays (multiple taggers all blocking)

### How It Works

Roslyn's taggers use `AbstractAsynchronousTaggerProvider` which implements a producer-consumer pattern:

1. **Events trigger recomputation** — Text changes, caret moves, workspace updates
2. **Events are batched** — Multiple rapid events collapse into one work item (configurable delay)
3. **Work runs on background thread** — UI thread stays responsive
4. **Results update incrementally** — Only changed spans notify the editor
5. **New events cancel old work** — If user keeps typing, stale computations are abandoned

The key insight: **prioritize responsiveness over accuracy**. Show approximate results quickly, refine when idle. Users tolerate brief inaccuracy; they don't tolerate lag.

### Event Sources

Taggers subscribe to events that should trigger recomputation:

- **Text changes** — Buffer content modified
- **Caret position** — User moved cursor (view taggers only)
- **Workspace changes** — Solution/project/document state changed
- **Options changes** — User changed editor settings

Events are composed via `TaggerEventSources`. A tagger declares what events it cares about, and the infrastructure handles batching and dispatch.

### Delays

Different scenarios need different responsiveness:

| Scenario | Delay | Rationale |
|----------|-------|-----------|
| Brace matching | Near-immediate | Must feel instant |
| Syntax coloring | Short | Very visible, needs speed |
| Semantic classification | Medium | Can layer on top of syntactic |
| Reference highlighting | Medium | Acceptable brief delay |
| Diagnostics | Longer | Expensive, less urgent |

The delay system prevents thrashing during rapid edits while ensuring eventual consistency.

---

## Tagger Lifecycle

### Creation

When the editor needs tags for a buffer, it:

1. Queries MEF for applicable `ITaggerProvider`s
2. Calls `CreateTagger<T>(buffer)` on each
3. Aggregates the results

But creating full taggers for every request would be expensive. Roslyn's providers use a **shared `TagSource`** pattern:

- First request: Create a `TagSource` that does the actual work, store it in buffer properties
- Subsequent requests: Return a lightweight `Tagger` wrapper around the shared `TagSource`
- Reference counting tracks how many consumers exist

This means one expensive computation serves multiple consumers (editor, error list, code lens, etc.).

### Recomputation

When a `TagSource` receives a triggering event:

1. **Debounce** — Wait for the configured delay (more events may arrive)
2. **Gather context** — Snapshot current state (document, caret position, visible spans)
3. **Switch to background** — Leave UI thread
4. **Compute new tags** — Call into Roslyn services
5. **Diff against cached tags** — Find what actually changed
6. **Notify editor** — Fire `TagsChanged` only for changed spans

The diffing step is critical: if 1000 tags exist but only 2 changed, only those 2 spans trigger editor updates.

### Disposal

When the last consumer disposes:

1. Reference count reaches zero
2. `TagSource` cancels any in-flight work
3. Unsubscribes from events
4. Removes itself from buffer properties
5. Releases resources

The reference counting ensures resources are held exactly as long as needed.

---

## Caching and Incremental Updates

### Why Caching Matters

A typical C# file might have thousands of classified spans. Recomputing all of them on every keystroke would be wasteful—most don't change.

### Tag Storage

Tags are stored in an **interval tree** (`TagSpanIntervalTree`), a data structure optimized for:

- Fast lookup by span (what tags overlap this region?)
- Efficient updates (add/remove without rebuilding)
- Span tracking (tags automatically adjust when text changes)

### Incremental Updates

When recomputing:

1. **Determine invalidated spans** — What regions could have changed?
2. **Keep tags outside invalidation** — If a tag is far from the edit, it's probably still valid
3. **Compute new tags only for invalidated spans**
4. **Merge** — Old unchanged tags + new tags = updated tag set

This turns O(file-size) work into O(change-size) work.

### Visibility Optimization

Taggers for non-visible buffers can pause:

- Hidden documents don't need up-to-date tags
- Work resumes when the document becomes visible
- Reduces resource usage for large solutions

---

## Major Tagger Types

Roslyn implements many taggers. Here are the main categories:

### Classification Taggers

Provide syntax coloring. Two layers:

- **Syntactic** — Based on parse tree. Fast, runs synchronously. Handles keywords, strings, comments.
- **Semantic** — Based on semantic model. Slower, runs async. Distinguishes class names from method names, marks static members, etc.

Syntactic provides immediate feedback; semantic refines it.

### Diagnostic Taggers

Show error/warning squiggles. Consume diagnostics from the diagnostic service (which handles the actual analysis). The tagger's job is just to map diagnostic spans to error tags.

Different taggers handle different diagnostic kinds (compiler syntax, compiler semantic, analyzer syntax, analyzer semantic) to enable appropriate delays and priorities.

### Highlighting Taggers

Visual feedback based on cursor position:

- **Reference highlighting** — All references to the symbol under cursor
- **Brace matching** — Matching brace/bracket/parenthesis pairs
- **Keyword highlighting** — Related keywords (if/else, try/catch)

These are view taggers (depend on caret position) and prioritize near-immediate response.

### Structure Taggers

Code folding and structural guides:

- **Outlining** — Collapsible regions for methods, classes, regions
- **Block structure** — Vertical lines showing scope

### Inline Hints Tagger

Displays parameter name hints and type hints directly in the editor (inlay hints). Shows things like parameter names at call sites (`DoSomething(/*name:*/ "foo")`) and inferred types for `var` declarations.

### Inline Diagnostics Tagger

Shows diagnostic messages inline in the editor, not just as squiggles. The actual error text appears at the end of the line or below it.

### String Indentation Tagger

Provides indentation guides for raw string literals. Helps visualize the indentation structure within multi-line strings.

### Brace Pairs Tagger

Colorizes matching brace pairs (rainbow braces). Different nesting levels get different colors to help visualize structure.

### Keyword Highlighter Tagger

Highlights related keywords when the cursor is on one. For example, placing the cursor on `if` highlights the matching `else`, or on `try` highlights `catch` and `finally`.

---

## LSP Migration Path

Many of these taggers have LSP protocol equivalents or will gain them over time. As LSP methods become available, these VS-specific taggers can and likely should be replaced with LSP-based implementations. This enables:

- Consistent behavior across VS and VS Code
- Reduced maintenance burden (one implementation serves both)
- Better alignment with the broader language tooling ecosystem

See [Language Server Overview](../language_server/product_overview.md) for the LSP architecture.

---

## Design Principles

### Responsiveness Over Precision

Users notice lag more than they notice a briefly-wrong classification. The system is tuned to:

- Show something fast (even if incomplete)
- Refine when idle
- Never block the UI thread

### Incremental Everything

- Incremental recomputation (only changed regions)
- Incremental notification (only changed tags)
- Incremental rendering (editor only redraws affected lines)

### Shared Work

- One `TagSource` serves multiple consumers
- Background computations are reused across taggers where possible
- Caching at multiple levels

### Graceful Degradation

- If semantic information isn't available, fall back to syntactic
- If the document is too large, skip expensive analysis
- If the user is typing rapidly, abandon stale work

---

## Common Patterns in Tagger Implementations

### The TagSource Split

Most taggers separate:
- **Provider** — MEF export, creates taggers, stateless
- **TagSource** — Shared computation engine, holds state
- **Tagger** — Lightweight wrapper, implements `ITagger<T>`

### Context Objects

Taggers receive context objects with:
- The document/snapshot to analyze
- Spans to tag (often just visible spans)
- Cancellation token

This pattern makes taggers testable (inject fake context) and enables optimizations (only compute for visible regions).

### Frozen Partial Semantics

Some taggers use "frozen partial semantics"—a fast but potentially incomplete semantic model. This enables:

1. **Fast first pass** — Show results with frozen semantics
2. **Accurate second pass** — Refine with full semantics when available

The user sees something quickly; accuracy follows.

---

## Related Documentation

- [Editor Integration Overview](./codebase_overview.md) — Parent document
- [Features Overview](../features/codebase_overview.md) — Services that taggers consume
- [Workspaces Overview](../workspaces/codebase_overview.md) — Document model that taggers operate on

---

## Documentation Scope

This document explains the tagger infrastructure: why it exists, how it's designed, and the patterns used. It focuses on what's specific to tagging, not the services taggers consume (diagnostics, classification, etc.—those are separate systems that feed data to taggers).

**What's covered:** Tagger architecture, async pattern, lifecycle, caching, incremental updates

**What's not covered:** Diagnostic computation, classification algorithms, semantic analysis internals

**Methodology:** This documentation was created using the [Codebase Explorer methodology](https://github.com/CyrusNajmabadi/codebase-explorer).

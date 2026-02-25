# Roslyn Incremental Parsing: A Deep Dive

This document explains how incremental parsing works in the Roslyn C# compiler. It is intended for
developers working on or with the Roslyn parser, and assumes familiarity with basic compiler
concepts.

For additional context, Neal Gafter's
[Toy-Incremental-Parser](https://github.com/gafter/Toy-Incremental-Parser/blob/main/README.md)
provides an educational reference implementation of similar techniques. Roslyn shares many of the
same themes but has its own production implementation.

## Why Incremental Parsing Matters

In an IDE, users expect instant feedback as they type. Every keystroke potentially changes the
syntax tree, and features like IntelliSense, error squiggles, and brace matching all depend on
having an up-to-date parse. For small files, reparsing from scratch is fast enough to be
imperceptible. But consider a 100,000-line test file, or a large API client. Reparsing the entire
file on every keystroke would introduce noticeable latency.

Incremental parsing solves this by reusing as much of the previous parse tree as possible. When a
user types a single character inside one method, we don't need to reparse the thousands of methods
before and after it.

## Foundations: The Roslyn Parser

Before diving into incrementality, we need to understand a few foundational aspects of how Roslyn
parses C#.

### Mostly Context-Free Recursive Descent

Roslyn uses a hand-written recursive descent parser. The parser is *mostly* context-free, meaning
that language productions like `ClassDeclaration`, `MethodDeclaration`, `Statement`, and
`Expression` correspond directly to parsing functions in
[`LanguageParser.cs`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs).

The "mostly" qualifier is important. C# has some context-sensitive constructs; for example, `await`
is only treated as a keyword inside an `async` method. The parser tracks this context via flags, and
as we'll see, this has implications for incremental parsing. But the overall design strives to
minimize context sensitivity to maximize incremental reuse potential.

### Green Nodes: Position-Free, Parent-Free

For a full detailed writeup of 'Green Nodes', see [Red-Green Trees](./Red-Green Trees.md).

Roslyn uses the "red/green tree" pattern. The parser produces **green nodes**, which are immutable
nodes that store only their *kind* and their *children*. Crucially, green nodes do **not** store:

- Absolute text positions (spans)
- Parent pointers

Instead, green nodes store only their *width* (the number of characters they span). This design
choice is fundamental to incremental parsing: because green nodes don't encode absolute positions,
they can be reused even when edits earlier in the file have shifted their position. A method
declaration at position 10,000 in the old tree can be reused at position 10,050 in the new tree
because the green node is identical either way.

More importantly, **reuse means literal object reuse**. When we say a node is "reused," we mean the
new tree points to the exact same object in memory. An enormous subtree from one parse can be used
*as is* in the next tree with zero copying. The only new allocations are the parent nodes along the
path from the root to the edit location.

This is what makes incremental parsing so efficient. A syntax tree can be tens of megabytes. Without
incremental parsing, every keystroke would allocate tens of megabytes. With incremental parsing, a
typical edit allocates only bytes: a handful of new parent nodes and some pointers. The cost becomes
commensurate with the *impact* of the edit (usually tiny) rather than the size of the file.

**Red nodes** are lazily constructed wrappers that provide the familiar API with spans and parent
navigation. They're built on-demand from green nodes when user code traverses the tree.

### Full-Fidelity Concrete Syntax Trees

For a full detailed writeup on 'Full-Fidelity', see [Red-Green Trees](./Red-Green Trees.md).

Roslyn produces *full-fidelity* syntax trees. Every character from the source file is represented
somewhere in the tree, including whitespace, comments, and even syntax errors. If you concatenate
all the tokens (including their trivia) in order, you get back the exact original source text, no
more, no less.

This property is essential for incremental parsing. Because the tree represents the *complete*
source text, nodes and tokens are literally isomorphic with the section of text that was parsed.
This means they can be safely reused as a proxy for the original text. The new text only needs to be
consulted when we cannot reuse old nodes or tokens for some reason.

### The List Pattern

Parse trees are hierarchical, with nodes containing lists of children:

- A `CompilationUnit` contains lists of members (namespaces, types, etc.)
- A `NamespaceDeclaration` contains lists of members
- A `ClassDeclaration` contains lists of type members (methods, properties, fields, etc.)
- A `BlockStatement` contains lists of statements

This repetitive pattern of "a parent containing a list of children" is important because it creates
natural boundaries where incremental reuse can occur.

## How Incremental Parsing Works

### The Incremental Parse Function

A normal (non-incremental) parse is a function from text to a syntax tree:

```
Parse: Text → SyntaxTree
```

An incremental parse is a function that takes the *new* text, the *old* tree, and the *change* that
transformed the old text into the new text:

```
IncrementalParse: (NewText, OldTree, TextChange) → NewSyntaxTree
```

The `TextChange` describes what region of the old text was replaced and with what. Given this
information, the incremental parser can identify which parts of the old tree are still valid and can
be reused.

### Token Reuse via the Blender

At the heart of incremental parsing is a component called the **blender**
([`Blender.cs`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/Blender.cs),
[`Blender.Reader.cs`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/Blender.Reader.cs)).
The blender's job is to provide tokens to the parser, drawing them either from the old tree (when
safe) or from the lexer (when necessary).

Think of the blender as maintaining a **cursor** into the old tree, tracking which position in the
*new* text corresponds to which location in the *old* tree. As the parser consumes tokens, the
blender advances this cursor.

#### Position Synchronization and the Change Delta

The blender tracks a
[`_changeDelta`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/Blender.Reader.cs#L30),
which is the cumulative difference in length between the old and new text up to the current
position. Before the edit location, positions in the old and new text are identical, so
`_changeDelta` is zero. After the edit, positions in the old tree need to be adjusted by this delta
to find the corresponding position in the new text.

For example, if the user replaced `1` with `true` at position 100, the delta is +3 (four characters
replacing one). Positions 0 through 99 are unchanged, but position 100 in the old tree now
corresponds to position 103 in the new text.

This bookkeeping allows the blender to know when the cursor is "synchronized," meaning the current
position in the new text aligns with a token or node boundary in the old tree (after adjustment).

#### How the Parser Knows Its Position

Even though green nodes don't store absolute positions, the parser always knows exactly where it is
in the new text. It starts at position 0 and accumulates the width of each token or node it
processes. At any moment, the parser knows its precise location.

Mapping this position back to the old text is trivial. If the parser is before the edit location,
the position is identical in both texts. If the parser is after the edit location, we simply apply
the delta to map back to the corresponding position in the old text.

#### How Blender Synchronization Works

The parser asks the blender for data given a position in the new text. The blender internally
translates this to the corresponding position in the old text.

The blender also always knows its own position in the old tree. It starts at position 0, and every
time it moves through a node or token, it knows the width it is moving and updates its position
accordingly.

This is how the blender determines whether it can return an old token or node. If blending brings it
to a node or token whose start position aligns with the mapped old-text position, it can return that
node or token. If not, the positions are out of sync, and the blender must fall back to lexing the
new text.

Lexing continues until the blender reaches a point where the old-text position aligns with a node or
token boundary again. At that point, reuse can resume.

For typical edits, this resynchronization happens almost immediately after the edited region.
However, more impactful edits (like inserting `/*`) can cause more churn before resynchronization
occurs.

#### When Tokens Can Be Reused

When the parser requests the next token, the blender checks:

1. Is the cursor synchronized with a token in the old tree?
2. Does that token fall entirely outside the edited region?
3. Does the token pass the reusability checks (more on this below)?

If all conditions are met, the blender returns the old token directly with no lexing required. This
saves both CPU time (skipping the lexer logic) and memory (reusing the existing token object).

#### Blending and Crumbling

The term "blending" (sometimes called "crumbling") describes how the old tree is broken down for
reuse. The blender maintains a queue of items from the old tree. Nodes at the front of the queue are
progressively broken into their children until tokens emerge.

The process works like this:

1. Start with the old tree's root node
2. When a node intersects the edit (or can't be reused for other reasons), **crumble** it: remove it
   from the queue and push its children instead
3. Continue until tokens are at the front of the queue
4. Return tokens to the parser as requested

This lazy crumbling means we only break down the parts of the tree we actually need to examine. Huge
subtrees that are entirely before or after the edit remain intact.

The output of this process is captured in a
[`BlendedNode`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/BlendedNode.cs)
structure that carries either a reused node, a reused token, or a freshly lexed token back to the
parser.

### Node Reuse at Strategic Points

Token reuse is valuable, but the real power of incremental parsing comes from **node reuse**.
Instead of reparsing an entire method declaration token by token, the parser can grab the whole
`MethodDeclarationSyntax` node from the old tree and reuse it wholesale.

This happens at **strategic points** in the parser, specifically in loops that parse lists of
high-level constructs:

- **Compilation unit members**: namespaces, top-level types, global statements
- **Type members**: methods, properties, fields, nested types, etc.
- **Statements**: the contents of method bodies and blocks

These points were chosen not only because they represent natural list boundaries, but also because
they are unaffected by lookahead concerns (explained below in [Why Not Expressions?](#why-not-expressions)).

Prime examples of this are:
- [`TryReuseStatement`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs#L8334)
  (called from `ParseStatementCore`)
- [`CanReuseMemberDeclaration`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs#L2442)
  (called when parsing type members)

At each of these points, the parser checks
[`IsIncrementalAndFactoryContextMatches`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs#L14289),
a property that verifies both that we're doing an incremental parse and that the parser's current
context matches the context in which the candidate node was originally parsed.

### Why Not Expressions?

You might notice that expressions are conspicuously absent from the list of reusable constructs.
This is deliberate, and relates to lookahead in the parser.

Expression parsing in C# involves significant lookahead, lookbehind, and context sensitivity:

- Is `<` the start of a generic argument list or a less-than operator?
- Is `(` a cast, a parenthesized expression, a tuple, a lambda, a deconstruction, or possibly more
  in the future?
- How does precedence affect grouping?

Because of this complexity, determining whether an expression node can safely be reused after an
edit is surprisingly difficult. An edit *after* an expression might retroactively change how that
expression should have been parsed. Rather than building elaborate (and error-prone) logic to detect
these cases, Roslyn takes the conservative approach: expressions are always reparsed.

In contrast, the strategic reuse points (members, statements) were chosen precisely because they
*don't* suffer from these lookahead issues. If a statement parsed without errors before (and thus
can be reused), it would not change to something else due to an edit that happens after that
statement. Validating this involved understanding the grammar and the parser implementation, then
informally verifying that lookahead is never needed past the termination point of these constructs.

Expressions being reparsed is a practical tradeoff. They are typically small, so reparsing them is
cheap. The big wins come from reusing statements and member declarations, which can be arbitrarily
large. (See [Caveats](#caveats) for edge cases where this assumption breaks down.)

## Worked Examples

### A Typical Edit

Consider a large class being edited:

```csharp
class HugeClass
{
    // ... Methods 0-499 ...

    void Method500()
    {
        // ... Statements 0-99 ...
        
        var x = 1;  // ← User changes "1" to "true"
        
        // ... Statements 101-200 ...
    }

    // ... Methods 501-1000 ...
}
```

The user has replaced `1` (1 character) with `true` (4 characters), so the change delta is +3.
Here's what happens during the incremental reparse:

1. **Initial traversal**: The blender walks down from the root, looking for the edit. It sees that
   `CompilationUnitSyntax` spans the edit, so it crumbles into its children.

2. **Reusing unaffected members**: Methods 0 through 499 don't intersect the edit. As the parser
   loops through type members, it calls `CanReuseMemberDeclaration` for each, and they're all reused
   wholesale with no reparsing of their contents.

3. **Crumbling the affected method**: Method 500 intersects the edit, so it can't be reused. The
   blender crumbles it into its children (modifiers, return type, name, parameters, body).

4. **Reusing unaffected statements**: Inside Method 500's body, statements 0 through 99 don't
   intersect the edit. As the parser loops through statements, it calls `TryReuseStatement` and
   reuses them all.

5. **Reparsing the affected statement**: The statement with `var x = 1` intersects the edit. It's
   reparsed token by token. (Tokens before the edit within this statement may still be reused from
   the old tree.)

6. **Reusing remaining statements**: Statements 101 through 200 are after the edit. The blender
   adjusts for the +3 delta, resyncs with the old tree, and these statements are reused.

7. **Reusing remaining members**: Methods 501 through 1000 are after the edit. They're all reused,
   again with position adjustment via the delta.

The result: out of potentially thousands of nodes and tens of thousands of tokens, only a handful
are actually reparsed. The new tree shares almost all of its green nodes (the actual objects in
memory) with the old tree.

### A More Impactful Edit

Not all edits are so localized. Consider what happens when someone types `/*` inside Method 250,
where there happens to be a matching `*/` inside Method 750:

```csharp
class HugeClass
{
    // ... Methods 0-249 ...

    void Method250() 
    { 
        /*  // ← User types this

    // ... Methods 251-749 (now inside a comment!) ...

    void Method750() 
    {
        var s = "*/";  // ← This closes the comment
        // ...
    }

    // ... Methods 751-1000 ...
}
```

In this case, the `/*` will invalidate tokens that follow it. When the lexer runs, it will produce a
single enormous multi-line comment token spanning from inside Method 250 all the way to Method 750.
Methods 251 through 749 are now *inside* that comment token and cannot be reused as members. The
closing brace `}` after the `*/` in Method 750 now closes Method 250 instead.

However, the system still works correctly. The blender will skip over vast swaths of the old tree
(which are now consumed by the comment token), resync after the `*/`, and resume normal incremental
parsing. Methods 751 through 1000 can still be reused as before.

This illustrates an important point: the cost of incremental parsing is commensurate with the
*impact* of the edit, not just its size. A simple `1 → true` edit has minimal syntactic impact, so
reuse is massive. Commenting out half the file has enormous syntactic impact, so the costs are
higher.

For typical low-impact edits (the vast majority of real-world typing), incremental parses complete
in *microseconds* with memory reuse approaching 99.99%. Higher-impact edits cost more, but even in
the worst case (commenting out an entire file), the cost degenerates to the same cost as a full
reparse. Incremental parsing is never appreciably worse than full parsing, while it is normally much
better.

## Correctness Constraints

The incremental parser is conservative. It only reuses nodes when it's *certain* they're still
valid. Several checks enforce this:

### Skipped Tokens

Skipped tokens, which are tokens the parser couldn't incorporate into a valid construct but had to
place in the tree to preserve full-fidelity invariants, are never reused. Their presence indicates
the parser encountered something unexpected, and that situation may have changed due to the edit.

To illustrate, consider a file with two methods where M1 contains skipped tokens (due to a syntax
error) and the user edits something in M2:

```csharp
class Example
{
    void M1()
    {
        int x = #;  // ← Skipped token here (the #)
    }

    void M2()
    {
        var y = 1;  // ← User changes "1" to "true"
    }
}
```

Even though the edit is entirely within M2, M1 is not eligible for reuse because it contains skipped
tokens. During the incremental reparse, M1 will be crumbled and reparsed. (Note that child
statements within M1 that don't themselves contain skipped tokens may still be reusable.)

In most cases, reparsing M1 will produce the same skipped tokens as before. However, it's possible
that the edit in M2 could affect M1's parse due to lookahead. For example, if M1's error was caused
by something that the edit in M2 somehow resolved, the reparse of M1 might produce a different
(possibly valid) tree.

This conservative approach does mean some extra reparsing and allocations. However, parse errors
tend to be rare, representing a tiny fraction of any tree. So this only marginally increases the
parsing cost in practice.

### Missing Nodes and Tokens

Missing nodes and tokens (inserted by the parser during error recovery when something was expected
but not found) are never reused. They're synthetic artifacts of the parsing process, not
representations of actual source text. The behavior is effectively the same as with skipped tokens:
we don't reuse nodes containing them for the same reasons.

### Other Diagnostics

Technically, the parser can produce other kinds of diagnostics beyond skipped and missing tokens.
These are rare, and we are actively working to move them out of the parser and into the binding
phase (as discussed in
[`Parser.md`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/docs/compilers/Design/Parser.md)).
Regardless, they are handled the same way: nodes containing any diagnostics are not reused and are
crumbled further.

### Context Sensitivity: Parser Flags

As mentioned earlier, C# has some context-sensitive parsing. For example:

- `await` is a keyword in `async` methods, an identifier otherwise
- `field` is a keyword inside property accessors (for the field keyword feature), an identifier
  otherwise

The parser tracks these contexts via bit flags stored on nodes. When considering a node for reuse,
the parser checks `IsIncrementalAndFactoryContextMatches`, which verifies that the current parsing
context matches the context that was active when the node was originally created. If they don't
match, the node is crumbled and reparsed.

### Synthetic Tokens

Some tokens are synthesized by the parser rather than the lexer. The canonical example is `>>`,
which might represent:

- The right-shift operator
- Two closing angle brackets in nested generics (`List<List<int>>`)

These tokens can't simply be reused from the old tree because their interpretation depends on
parsing context. When encountered, they need to be re-examined.

## Caveats

The incremental parser is optimized for the expected use case: real C# files being actively edited
by developers. Certain edge cases can degrade performance.

### Giant Expressions

The assumption that expressions are small does not always hold. A user with an array initializer
containing a million elements, or a massive interpolated string, will only get token reuse when that
expression is edited. No node reuse occurs within expressions.

This can cause high churn in both CPU and memory. Practically, this is rare, but not nonexistent.
Tooling and profiling can help reveal these cases. The general advice is to break giant expressions
down into smaller pieces (for example, building an array from smaller arrays) to make incremental
parsing more effective.

### Files with Pervasive Errors

Incremental parsing assumes that most of the tree is well-formed. Files with syntax errors scattered
throughout will have many nodes containing diagnostics or skipped tokens, and those nodes won't be
reusable. In extreme cases (random text, binary files accidentally opened as C#), almost nothing
will be reusable.

These scenarios are far outside the norm. We are optimizing for real C# files, not arbitrary text.

### Generated Files

Generated files (source generators, T4 templates, etc.) are not a concern for incremental parsing
performance. Users don't edit generated files in real time, so we won't be receiving incremental
edits for them. Full parses of generated files are acceptable since they happen infrequently
(typically only when the generator runs).

Razor files are a notable exception. Although Razor uses source generation, the generated C# code
regenerates on every keystroke as the user edits. This means Razor files are fully reparsed each
time rather than incrementally parsed. For small Razor files, this is not noticeable. However, as
users write larger Razor pages, full reparses could become a bottleneck.

A potential path forward would be for Razor to avoid reparsing the full generated file in isolation.
Instead, it could first perform a very fast diff (linearly comparing the matching prefix and suffix
of the old and new text to detect the start of the first change and the end of the last change),
then feed that change region into an incremental parse. Since most edits affect a small region of
the generated output, this could significantly reduce parsing costs for large Razor files.

### Deeply Nested Constructs

Deeply nested constructs that don't hit strategic reuse points (statements, members) won't benefit
from node reuse. For example, thousands of nested `if` statements without braces, or deeply nested
ternary expressions. This is somewhat pathological and rare in practice, but worth noting.

## Key Code Locations

For those wanting to explore the implementation:

### Core Files

- [`LanguageParser.cs`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs):
  The main recursive-descent parser
- [`Blender.cs`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/Blender.cs):
  Orchestrates token/node supply from old tree or lexer
- [`Blender.Reader.cs`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/Blender.Reader.cs):
  The `Reader` struct that implements cursor logic and reuse checks

### Key Methods and Properties

This list is not exhaustive, but serves to give a good high-level idea of the major points:

- [`TryReuseStatement`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs#L8334):
  Called when parsing statements; attempts to reuse a statement node
- [`CanReuseMemberDeclaration`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs#L2442):
  Called when parsing type members
- [`IsIncrementalAndFactoryContextMatches`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/LanguageParser.cs#L14289):
  Guards node reuse with context checks
- [`CanReuse`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/Blender.Reader.cs#L215)
  (in `Blender.Reader`): Checks all constraints on token/node reusability
- [`_changeDelta`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Parser/Blender.Reader.cs#L30):
  Tracks position drift between old and new text

### Related Documentation

- [`docs/compilers/Design/Parser.md`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/docs/compilers/Design/Parser.md):
  Design guidelines for the parser, including notes on how diagnostic placement affects incremental
  parsing
- [Red-Green Trees](./Red-Green Trees.md).  A deep dive into Roslyn's Red/Green syntax node split, with a heavy emphasis on the internal Green side.

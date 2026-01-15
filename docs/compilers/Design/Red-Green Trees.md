# Roslyn's Syntax Model Internals: Red/Green Trees

This document explains the internal architecture of Roslyn's syntax model, focusing on the "green"
layer that powers its efficiency. It is intended for developers working on or with the Roslyn
compiler internals.

For the public API and general usage of syntax trees, see the official documentation:
[Use the .NET Compiler Platform SDK syntax model](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-syntax).
This document assumes familiarity with that material and dives into the implementation details that
make the syntax model performant.

## The Red/Green Pattern

Roslyn's syntax trees use a design pattern sometimes called "red/green trees." The core idea is to
maintain two parallel representations of the same syntactic structure:

- **Green nodes**: The internal, immutable representation. These nodes store only their syntactic
  *kind*, their *width* (character count), and their *children*. They do not store absolute text
  positions or parent pointers.

- **Red nodes**: The public-facing wrappers. These provide the familiar API with properties like
  `Span`, `Parent`, and strongly-typed child accessors. Red nodes are thin wrappers that combine a
  green node with positional and parental context.

This split exists because immutability and efficient navigation are fundamentally at odds. An
immutable node cannot store a parent pointer (since the same node might have different parents in
different contexts). But users need parent navigation. The red/green pattern solves this: green
nodes are immutable and shareable, while red nodes provide the navigation API by computing positions
and parents on demand.

## Green Nodes: The Core Design

A green node stores:

- **Kind**: What type of syntax this is (e.g., `MethodDeclaration`, `IdentifierToken`)
- **Width**: The total number of characters spanned by this node and all its descendants
- **Children**: References to child green nodes (via slots)
- **Diagnostics**: Any syntax errors attached to this node

Crucially, green nodes do **not** store:

- **Absolute positions**: A green node has no idea where it sits in the source file
- **Parent pointers**: A green node has no idea what contains it

This design choice is fundamental. Everything else flows from it.

### Slot-Based Child Access

Green nodes use integer "slots" to access children. Each green node type knows how many slots it has
and what kind of child belongs in each slot. For example, a green `MethodDeclarationSyntax` node
might have slots for: attributes (0), modifiers (1), return type (2), identifier (3), type
parameters (4), parameters (5), constraints (6), body (7), and so on.

For every red syntax node type, there is a corresponding internal green syntax node type. So there
is both a public red `MethodDeclarationSyntax` and an internal green `MethodDeclarationSyntax`. The
green type can be accessed by slot index for uniform traversal, or by strongly-typed properties that
correspond to each child. The red layer accesses these green properties directly when producing
child nodes through the red API.

This design enables internal algorithms to walk the green tree generically by slot index (without
knowing what node type they're visiting), while also providing efficient strongly-typed access when
the node type is known.

## Benefits of Position-Free, Parent-Free Nodes

The decision to omit positions and parents from green nodes enables several powerful optimizations.

### Incremental Parsing and Subtree Reuse

When a user edits a file, incremental parsing can reuse vast portions of the previous syntax tree.
Because green nodes don't encode absolute positions, a method declaration that was at position
10,000 can be reused at position 10,050 after an earlier edit. The green node is identical; only the
red wrapper's computed position changes.

This enables incremental parses to complete in microseconds with memory reuse approaching 99.99% for
typical edits. For full details, see [Roslyn Incremental Parsing: A Deep Dive](./Incremental%20Parser.md).

### Cross-Tree Sharing

The same green node can appear in completely unrelated syntax trees. Consider the `ParameterList`
node for an empty parameter list `()`. This exact construct appears in thousands of methods across a
large solution. Because green nodes have no position or parent, the *same* green node object can be
shared across all of them.

This sharing happens automatically through green node caching (described below). The result is
significant memory savings at the solution level.

### Intra-Tree Sharing: It's a DAG, Not a Tree

The same green node can appear multiple times within a *single* syntax tree. If a file contains ten
methods that all have empty parameter lists `()`, all ten can point to the same `ParameterList`
green node.

This means "green tree" is actually a misnomer. The green structure is an **directed acyclic graph**
(DAG), not a tree. Multiple parents can share the same child. This dramatically reduces memory for
files with repeated constructs.

The red layer hides this complexity. Each red node appears to be a distinct object with its own
unique parent and position, even though the underlying green nodes may be shared.

## Everything Is a Node at the Green Level

In the public red API, the syntax model uses different types for different concepts:

- `SyntaxNode`: A class (reference type) for syntax nodes
- `SyntaxToken`: A struct (value type) for tokens
- `SyntaxTrivia`: A struct for whitespace and comments
- `SyntaxList<T>`: A struct for lists of nodes

In the green layer, **all of these are heap-allocated node objects**. There are no structs. Every
green token, every piece of green trivia, every green list is a `GreenNode` subclass.

Why? Because structs cannot participate in the sharing scenarios described above. If tokens were
structs at the green level, reusing an identifier across multiple locations would require copying
the struct each time. With green nodes as objects, we can instead point to the same instance.

Beyond sharing, this choice also enables an important aspect of the green layer’s design:
specialization. The green tree contains many specialized implementations tailored for different
scenarios (for example, optimized token representations and specialized list implementations).
This specialization is essential for achieving good performance and memory usage, and will be
discussed in more detail below.

Structs are not well suited to this role, as they cannot participate in inheritance hierarchies
or support multiple specialized implementations behind a common abstraction. By representing
green nodes as objects, we can use standard inheritance techniques to freely specialize the
internal representation, while still exposing a simple, lightweight, struct-based API at th
e public syntax level. The public structs effectively act as thin facades over these underlying
green nodes.

The consequence is that red wrappers are extremely cheap. A red `SyntaxToken` struct is just:

- A pointer to the parent red node
- An integer position
- A pointer to the underlying green token node

No heap allocation occurs when you access a token. The same applies to trivia and lists at the red
level.

Since tokens, lists, and trivia typically constitute **75% or more** of a syntax tree's elements,
this design avoids the vast majority of allocations that would otherwise occur when traversing a
tree.

## List Optimizations

Lists are ubiquitous in syntax trees: parameter lists, argument lists, statement lists, modifier
lists, and so on. Roslyn heavily optimizes list representation at the green level.

### Empty Lists

An empty list is represented as `null` at the green level. No allocation whatsoever. When wrapped by
a red `SyntaxList<T>`, it checks for null and returns `Count = 0`.

### Singleton Lists

A list with exactly one element doesn't allocate a green list node. Instead, the parent green node
points directly to the single child element. The red `SyntaxList<T>` detects that it's pointing at a
non-list green node and returns `Count = 1`, providing access to just that element.

### Small Lists (2, 3, 4 Elements)

Lists of length 2, 3, and 4 have specialized green node subclasses (`WithTwoChildren`,
`WithThreeChildren`, etc.). These subclasses store child pointers directly in fields rather than
using an array. This avoids both the array allocation and the indirection of accessing array
elements.

### Larger Lists

Lists with 5 or more elements use an array-backed representation.

#### Why This Matters

In practice, **90% or more of lists contain 4 elements or fewer**. Think about how often you write
methods with 0, 1, 2, or 3 parameters versus methods with 5+ parameters. Argument lists, type
parameter lists, attribute lists—all follow similar distributions.

By optimizing for the common case, Roslyn avoids array allocations for the overwhelming majority of
lists. The red `SyntaxList<T>` and `SeparatedSyntaxList<T>` structs handle all these cases
transparently, presenting a uniform API regardless of the underlying representation.

### Weakly-held red children

Certain red lists are able to hold their red children using weak references. This allows portions of
the red tree to be reclaimed by the GC when they are no longer strongly reachable, without affecting
correctness.

This is safe because a red node can only be collected if nothing is holding a strong reference to it.
If the only remaining reference is the weak reference held by the parent list, then there is no possible
observer that could notice the red node going away. If the child is later requested again, a new red
node will be created from the same underlying green node. Since red nodes are purely a cache over
immutable green structure, this behavior is unobservable and semantically equivalent to retaining the
original red node.

#### Current usage

Currently, this optimization is applied only to blocks (`{ ... }`) that are owned by a member or
accessor ([see here](https://github.com/dotnet/roslyn/blob/744249e4f0afb645808e87aebec109a38d5dde8b/src/Compilers/CSharp/Portable/Syntax/CSharpSyntaxNode.cs#L518)).
These blocks can be large, and are often only needed temporarily during analysis. By allowing
the red lists for such blocks to weakly reference their children, large amounts of red memory can be
released once nothing else is holding onto those nodes, reducing peak memory usage while preserving
the existing red/green tree semantics.

## Green Node Caching

Roslyn maintains a cache of commonly-occurring green nodes. When constructing a new green node
during parsing, the parser first checks if an equivalent node already exists in the cache. If so, it
reuses the cached node instead of allocating a new one.

Due to combinatorial explosion, only nodes with **3 or fewer children** are eligible for caching.
The cache itself holds 65,536 entries. Despite these limitations, analysis of parsing the Roslyn
codebase itself shows a **55% cache hit rate** for cacheable nodes. This means over half the time,
the parser can reuse an existing allocation rather than creating a new one.

For implementation details, see [PR #80825](https://github.com/dotnet/roslyn/pull/80825) which
simplified and analyzed the `SyntaxNodeCache`.

This caching is what enables the cross-tree and intra-tree sharing described earlier. The `()`
parameter list isn't just theoretically shareable—it's *actually* shared because the cache returns
the same node every time that construct is parsed.

## Token Storage Optimizations

Because green nodes are internal, we have freedom to optimize their storage in ways that would be
awkward to expose publicly. Tokens illustrate this well.

### Identifier Tokens

An identifier token stores a pointer to its text (e.g., `"myVariable"`). Since the text is stored,
the width doesn't need separate storage—it can be computed from the string length. This saves space
on every identifier in the tree.

### Keyword Tokens

Keyword tokens work in reverse. All `return` keywords have the exact same text, so there's no need
to store it. Instead, keyword tokens store only their *kind*, and the text and width are inferred
from that. A `return` keyword token knows it's `SyntaxKind.ReturnKeyword`, and the text `"return"`
and width `6` follow automatically.

### Pre-Cached Keyword Variants

Roslyn pre-computes and caches every keyword token without trivia, as well as variants with common
leading and trailing whitespace patterns. Consider `void ` (the keyword `void` followed by a
trailing space). This exact construct appears frequently in source files, and every occurrence
points to the same literal green node instance. The parser doesn't allocate anything—it just returns
the pre-cached node.

These optimizations are invisible to users of the red API, but they significantly reduce memory
usage and allocation pressure during parsing.

## Red Node Creation: Lazy and Cached

Red nodes are created on-demand when you traverse the syntax tree. If you never access a particular
node, its red wrapper is never created.

For red `SyntaxNode` instances (the class type), caching is required once created. This is necessary
because nodes have reference identity—users expect that accessing the same child twice returns the
same object. The caching is straightforward: when a red parent node is asked for a child, it checks
if the child red node has already been created. If not, it instantiates the child and atomically
writes it into its field storage. Whichever thread wins the race sets the pointer that all
subsequent reads will see.

For red `SyntaxToken`, `SyntaxTrivia`, and `SyntaxList<T>` (all struct types), no caching is needed.
Each access returns a new struct instance, but structs have value semantics, so this is fine. Two
red structs pointing to the same green node at the same position are considered equal.

### Implications for Performance

Features that don't walk the entire tree pay only for what they touch. Consider IDE colorization: it
only needs to examine the tokens visible on screen. The red nodes for methods scrolled off-screen
are never created (or, if previously created, may be collected if memory pressure occurs).

Combined with the massive green reuse from incremental parsing, this means the syntax layer
allocates very little under normal usage patterns:

- Green nodes are heavily reused across edits and across files
- Tokens, lists, and trivia (75%+ of the tree) have no red allocations
- Red node allocations are deferred until actually needed
- Only the portion of the tree being actively examined incurs red node costs

## Immutability

Green nodes are fully immutable. Once created, a green node never changes. This has several
benefits:

- **Thread safety**: Multiple threads can read the same syntax tree without locks
- **Predictability**: Once you have a reference to a node, it will never change out from under you
- **Sharing**: Immutable objects can be freely shared without defensive copying

When you need to "modify" a syntax tree (via `WithXxx` methods or `SyntaxFactory`), you get a new
tree that shares as much structure as possible with the old tree. Only the nodes along the path from
the root to the modification point are newly allocated; everything else is reused.

For more on working with immutable syntax trees, see the
[official documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-syntax).

## Full Fidelity

Roslyn syntax trees are *full-fidelity*: every character from the source file is represented
somewhere in the tree. Whitespace, comments, preprocessor directives, and even malformed syntax are
all preserved. Concatenating all tokens and trivia in order reproduces the original source exactly.

This property is essential for tooling scenarios. Refactoring tools can modify specific parts of the
tree while preserving the user's formatting elsewhere. Error recovery ensures that partially-written
code still produces a navigable tree.

Full fidelity also enables incremental parsing to reason precisely about which portions of the tree
are affected by an edit. See
[Roslyn Incremental Parsing: A Deep Dive](./Incremental%20Parser.md) for details.

## Key Code Locations

For those wanting to explore the implementation:

### Green Layer (Internal)

- [`GreenNode`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/Core/Portable/Syntax/GreenNode.cs):
  The base class for all green nodes, defining slot-based child access
- [`SyntaxNodeCache`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/Core/Portable/Syntax/SyntaxNodeCache.cs):
  The cache enabling green node reuse across and within trees
- [`Syntax/InternalSyntax/SyntaxList.cs`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/Core/Portable/Syntax/InternalSyntax/SyntaxList.cs):
  Green list base class with `WithTwoChildren`, `WithThreeChildren`, etc. subclasses
- [`Syntax/InternalSyntax/`](https://github.com/dotnet/roslyn/tree/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/Core/Portable/Syntax/InternalSyntax):
  Folder containing green node infrastructure and list helpers

Language-specific green nodes live in their respective compiler directories:
- [`CSharp/Portable/Syntax/InternalSyntax/`](https://github.com/dotnet/roslyn/tree/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/CSharp/Portable/Syntax/InternalSyntax):
  C# green tokens, trivia, and syntax factory

### Red Layer (Public API)

- [`SyntaxNode`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/Core/Portable/Syntax/SyntaxNode.cs):
  The public red node base class
- [`SyntaxToken`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/Core/Portable/Syntax/SyntaxToken.cs):
  The public red token struct
- [`SyntaxTrivia`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/Core/Portable/Syntax/SyntaxTrivia.cs):
  The public red trivia struct
- [`SyntaxList<T>`](https://github.com/dotnet/roslyn/blob/b2cfaaf967aaad26cd58e7b2cc3f2d9fcede96f4/src/Compilers/Core/Portable/Syntax/SyntaxList%601.cs):
  The public red list struct

### Design Philosophy

The codebase contains many specialized subclasses for different node configurations, list sizes, and
token types. This allows heavy optimization at the green level. However, the power of the red/green
split is that all this complexity is hidden from users. The public API presents a clean, intuitive
syntax tree with parent navigation and absolute positions. Users don't need to know about green
nodes, slot-based access, list representation tricks, or caching strategies.

Yet because of these internal optimizations, combined with the usage patterns described above,
performance is excellent:

- Tokens, lists, and trivia incur no red allocations
- Red node allocations are minimal due to lazy creation
- Green nodes are heavily shared within and across trees
- Incremental parsing reuses nearly everything across edits

The complexity lives in the implementation so that users get both a pleasant API and great
performance.

# Roslyn Glossary

**Last Updated:** January 29, 2026

This glossary defines internal terminology, codenames, and acronyms used throughout the Roslyn codebase. Terms are organized alphabetically within categories.

---

## How to Use This Glossary

- **Ctrl+F** to find specific terms
- Terms in **bold** are internal codenames (vs. industry-standard terms)
- Links point to relevant documentation or code
- "See also" suggests related terms

---

## Internal Codenames & Components

These are names that won't appear in external documentation—you need to know them to read the code and communicate with the team.

| Name | What It Actually Is | Code Location |
|------|---------------------|---------------|
| **Roslyn** | The .NET Compiler Platform (original codename, still commonly used) | Entire repo |
| **VBCSCompiler** | Compiler server daemon that keeps compilers warm | `src/Compilers/Server/` |
| **CLaSP** | Common Language Server Protocol Framework | `src/LanguageServer/Microsoft.CommonLanguageServerProtocol.Framework/` |
| **ServiceHub** | VS out-of-process communication framework | `src/Workspaces/Remote/` |
| **Arcade** | Microsoft's shared build infrastructure SDK | `eng/` |
| **BuildBoss** | Tool that validates solution/project structure | `src/Tools/BuildBoss/` |
| **BuildValidator** | Tool that validates build determinism | `src/Tools/BuildValidator/` |

---

## Acronyms

| Acronym | Expansion | Description |
|---------|-----------|-------------|
| **API** | Application Programming Interface | Public surface exposed by Roslyn |
| **AST** | Abstract Syntax Tree | Synonym for Syntax Tree in compiler contexts |
| **BoundTree** | Bound Tree | Semantic tree after binding (internal representation) |
| **CLR** | Common Language Runtime | .NET execution engine |
| **DKM** | Debugger Kernel Model | VS debugger engine integration point |
| **IL** | Intermediate Language | .NET bytecode emitted by compilers |
| **LSP** | Language Server Protocol | Protocol for editor-agnostic language features |
| **MEF** | Managed Extensibility Framework | Dependency injection used by IDE services |
| **PDB** | Program Database | Debug symbol file format |
| **PE** | Portable Executable | Windows executable/DLL format |
| **REPL** | Read-Eval-Print Loop | Interactive scripting environment |
| **TFM** | Target Framework Moniker | e.g., `net8.0`, `netstandard2.0` |
| **VSIX** | Visual Studio Extension | Package format for VS extensions |

---

## Compiler Terms

### A-D

**Binder**  
Component that performs semantic analysis: resolves names to symbols, checks types, infers types for `var` and lambdas. The Binder produces the BoundTree.  
*See also: BoundTree, Symbol*

**BoundTree**  
Internal semantic representation after binding. Contains `BoundNode`s with full type and symbol information. Language-specific (C# and VB have different BoundTree hierarchies).  
*See also: IOperation, SyntaxTree*

**Compilation**  
Immutable object representing a complete compilation unit. Contains `SyntaxTree`s, `MetadataReference`s, and `CompilationOptions`. Lazily computes semantic information.  
*See also: SemanticModel, SyntaxTree*

**Conversion**  
Type conversion in semantic analysis. Can be implicit, explicit, user-defined, or built-in (numeric, reference, boxing, etc.).

**Decision DAG**  
Directed Acyclic Graph data structure used for compiling pattern matching expressions efficiently.

### E-L

**Emit**  
Phase that generates IL from the lowered BoundTree. Produces PE and PDB files.  
*See also: Lowering, PEWriter*

**ErrorType**  
Placeholder symbol type used when binding fails. Allows compilation to continue and report multiple errors.

**Flow Analysis**  
Analysis that tracks variable state (assignment, nullability) through control flow paths. Used for definite assignment, nullable reference types, etc.

**IOperation (Semantic AST)**  
Language-agnostic semantic representation, commonly referred to as an "Abstract Syntax Tree" (AST) in compiler literature and the broader ecosystem. Unlike BoundTree (which is internal and language-specific), IOperation is public API used by analyzers.  
*See also: BoundTree*

**Lexer / Scanner**  
First compilation phase that tokenizes source text into `SyntaxToken`s. C# uses "Lexer"; VB uses "Scanner" (same concept, different names).  
*See also: SyntaxToken, Trivia*

**Lowering**  
Phase that transforms high-level language constructs (async/await, iterators, pattern matching) into simpler IL-ready forms. Happens after binding, before emit.  
*See also: LocalRewriter, AsyncRewriter*

### M-R

**MetadataReference**  
Reference to external assembly metadata (DLLs). Can be file-based or in-memory.

**Nullability**  
Nullable reference type analysis. Tracks whether expressions can be null and emits warnings for potential null dereferences.

**Overload Resolution**  
Algorithm for choosing the best method from a set of candidates based on argument types.

**Parser**  
Compilation phase that builds `SyntaxTree` from tokens. Produces syntax nodes representing the grammatical structure.  
*See also: SyntaxTree, SyntaxNode*

**PEWriter**  
Component that writes PE (Portable Executable) files—the final output format for assemblies.

### S-Z

**SemanticModel**  
API for querying semantic information about syntax nodes: `GetSymbolInfo()`, `GetTypeInfo()`, `GetOperation()`. Caches binding results.  
*See also: Compilation, Symbol*

**Spill Sequence**  
Temporary variables introduced during lowering to preserve evaluation order when expressions have side effects.

**State Machine**  
Generated class for async methods and iterators. Contains the suspension/resumption logic.

**Symbol**  
Represents a named entity: type, method, property, field, parameter, etc. `ISymbol` is the root interface; derived interfaces include `INamespaceSymbol`, `ITypeSymbol`, `IMethodSymbol`, `IPropertySymbol`, `IFieldSymbol`, `IParameterSymbol`, `ILocalSymbol`, etc. Has source symbols (from code) and metadata symbols (from references).

**Synthesized**  
Compiler-generated symbols or members not explicitly in source (e.g., default constructors, anonymous type members).

**SyntaxFactory**  
Factory class for creating syntax nodes programmatically.

**SyntaxNode**  
Node in the syntax tree representing a grammatical construct (class declaration, method call, etc.). Immutable.  
*See also: SyntaxTree, SyntaxToken*

**SyntaxToken**  
Terminal node in the syntax tree (keyword, identifier, literal, punctuation). Contains leading/trailing trivia.  
*See also: Trivia*

**SyntaxTree**  
Immutable parsed representation of source code. Contains a root `SyntaxNode` and source text.  
*See also: Compilation, SyntaxNode*

**Trivia**  
Whitespace, comments, and preprocessor directives attached to tokens. Essential for preserving formatting during code transformations.

**Type Inference**  
Inferring types from context: `var` declarations, lambda parameter types, generic type arguments.

**Use-Site / Declaration-Site**  
Where a symbol is used vs. where it's declared. Important for diagnostic reporting.

---

## Workspace Terms

**Document**  
Represents a source file in a project. Contains syntax tree, semantic model, and source text.  
*See also: Project, Solution*

**DocumentId**  
Unique identifier for a document within a solution.

**Host Services**  
Services provided by the host environment (VS, VS Code, etc.). Accessed via `Workspace.Services`.

**MSBuildWorkspace**  
Workspace implementation that loads solutions/projects via MSBuild.

**Project**  
Collection of documents with shared compilation options and references.  
*See also: Document, Solution*

**ProjectId**  
Unique identifier for a project within a solution.

**Remote Workspace**  
Out-of-process workspace mirror for ServiceHub services. Synchronizes via checksums.

**Solution**  
Immutable snapshot of all projects and documents. All workspace operations produce new `Solution` instances.  
*See also: Document, Project*

**SolutionState / ProjectState / DocumentState**  
Internal immutable state containers. Public `Solution`/`Project`/`Document` are wrappers.

**VersionStamp**  
Tracks when documents/projects/solutions change. Used for caching and incremental updates.

**Workspace**  
Abstract base for managing solutions. Subclasses: `MSBuildWorkspace`, `AdhocWorkspace`, `VisualStudioWorkspace`.

**WorkspaceChangeKind**  
Enum describing workspace change type: `DocumentChanged`, `ProjectAdded`, etc.

---

## IDE Feature Terms

**Code Action**  
Refactoring or code fix offered in the lightbulb menu. Base class: `CodeAction`.

**Code Fix**  
Code action that fixes a diagnostic. Registered via `ExportCodeFixProvider`.

**Code Refactoring**  
Code action for restructuring code (not tied to a diagnostic). Registered via `ExportCodeRefactoringProvider`.

**Completion Provider**  
Provides IntelliSense completion items. Registered via `ExportCompletionProvider`.

**Diagnostic**  
Error, warning, or info message. Has an ID (e.g., `CS1002`, `IDE0001`), severity, location, and message.

**Diagnostic Analyzer**  
Component that produces diagnostics. Registered via `DiagnosticAnalyzer` base class.

**Fix All Provider**  
Applies a code fix across multiple locations (document, project, solution).

**Language Service**  
Per-language service implementation. Registered via `ExportLanguageService`.

**Light Bulb**  
UI term for the code action menu (💡 icon in editors).

**Provider**  
Extensible component (CompletionProvider, CodeFixProvider, etc.). Discovered via MEF.

**Quick Info**  
Hover tooltip showing symbol information.

**Semantic Document**  
Document wrapper with pre-fetched semantic model. Common in feature implementations.

**Signature Help**  
Parameter hints shown when typing method arguments.

**Solution Crawler**  
Background analysis system that incrementally analyzes documents as they change.

---

## Editor Integration Terms

**Adornment**  
Visual overlay in the editor (inline hints, diagnostic squiggles).

**Command Handler**  
Handles editor commands (format, rename, etc.). Interface: `ICommandHandler`.

**Subject Buffer**  
The `ITextBuffer` being operated on.

**Tagger**  
Provides tags for text spans (classification, diagnostics). Interface: `ITagger<T>`.

**Tag Source**  
Internal state management for taggers.

**Text Buffer**  
VS editor's mutable text storage. Interface: `ITextBuffer`.

**Text Snapshot**  
Immutable snapshot of text buffer contents. Interface: `ITextSnapshot`.

**Text View**  
Editor viewport showing text. Interface: `ITextView`.

**Viewport Tagger**  
Tagger optimized for visible viewport only.

---

## LSP Terms

**Handler**  
Processes LSP requests/notifications. Annotated with `[Method("methodName")]`.

**Mutating Request**  
LSP request that modifies solution state (must be serialized).

**Request Context**  
Per-request context with Solution, Document, capabilities.

**Request Execution Queue**  
Coordinates request execution; serializes mutating requests.

---

## Build/Test Terms

**Arcade SDK**  
Microsoft's shared build infrastructure. Provides CI templates, signing, packaging.

**Bootstrap Compiler**  
Building Roslyn using itself. First builds compiler, then uses it for the rest.

**Helix**  
Microsoft's distributed test execution system.

**Solution Filter (`.slnf`)**  
JSON file that loads a subset of projects from a solution.

---

## Deprecated/Legacy Terms

| Old Term | Current Equivalent | Notes |
|----------|-------------------|-------|
| Red/Green Trees | Syntax Trees | Historical name for the immutable syntax tree design |
| InternalSyntax | Green Nodes | Internal representation; "red" nodes are the public wrappers |

---

## External Technologies

| Technology | What It Is | Our Usage | Docs |
|------------|------------|-----------|------|
| **MEF** | Managed Extensibility Framework | DI for IDE services | [MEF Docs](https://docs.microsoft.com/en-us/dotnet/framework/mef/) |
| **LSP** | Language Server Protocol | VS Code integration | [LSP Spec](https://microsoft.github.io/language-server-protocol/) |
| **MSBuild** | Build system | Project loading, build | [MSBuild Docs](https://docs.microsoft.com/en-us/visualstudio/msbuild/) |
| **xUnit** | Test framework | Unit testing | [xUnit Docs](https://xunit.net/) |
| **Arcade** | Build SDK | CI/CD infrastructure | [Arcade Repo](https://github.com/dotnet/arcade) |

---

## Expanding This Documentation

This glossary covers high-level terminology. For deeper exploration:

- Ask an AI assistant to "drill into [specific area]" for detailed component-level documentation
- See the [Codebase Explorer methodology](https://github.com/CyrusNajmabadi/codebase-explorer) for guided deep-dives

**Existing Roslyn Glossary:**
- [IDE Glossary](../ide/glossary.md) — IDE-specific terms

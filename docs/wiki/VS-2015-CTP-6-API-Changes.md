#API changes between VS 2015 CTP5 and VS 2015 CTP6

##Diagnostics and CodeFix API Changes
- DiagnosticAnalyzerAttribute's parameterless constructor has been removed. When no language was passed in, we were treating that to mean the analyzer works for any language which is not a guarantee anyone can make. 

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis.Diagnostics {
    public sealed class DiagnosticAnalyzerAttribute : Attribute {
-     public DiagnosticAnalyzerAttribute();
-     public DiagnosticAnalyzerAttribute(string supportedLanguage);
+     public DiagnosticAnalyzerAttribute(string firstLanguage, params string[] additionalLanguages);
+     public string[] Languages { get; }
-     public string SupportedLanguage { get; }
    }
  }
}
```

- The CodeFixes\CodeRefactoring APIs were cleaned up for consistency. `CodeAction.Create`'s overloads which took Document\Solution were removed because they lead to poor performance in the lightbulb.

```diff
assembly Microsoft.CodeAnalysis.Workspaces {
  namespace Microsoft.CodeAnalysis.CodeActions {
    public abstract class CodeAction {
+     public virtual string EquivalenceKey { get; }
-     public virtual string Id { get; }
-     public static CodeAction Create(string description, Document changedDocument, string id=null);
-     public static CodeAction Create(string description, Solution changedSolution, string id=null);
      public static CodeAction Create(string descriptiontitle, Func<CancellationToken, Task<Document>> createChangedDocument, string idequivalenceKey=null);
      public static CodeAction Create(string descriptiontitle, Func<CancellationToken, Task<Solution>> createChangedSolution, string idequivalenceKey=null);
-     public Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(CancellationToken cancellationToken);
+     public Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(CancellationToken cancellationToken);
-     public Task<IEnumerable<CodeActionOperation>> GetPreviewOperationsAsync(CancellationToken cancellationToken);
+     public Task<ImmutableArray<CodeActionOperation>> GetPreviewOperationsAsync(CancellationToken cancellationToken);
-     protected Task<IEnumerable<CodeActionOperation>> PostProcessAsync(IEnumerable<CodeActionOperation> operations, CancellationToken cancellationToken);
+     protected Task<ImmutableArray<CodeActionOperation>> PostProcessAsync(IEnumerable<CodeActionOperation> operations, CancellationToken cancellationToken);
    }
    public abstract class CodeActionWithOptions : CodeAction {
+     protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken);
    }
  }
  namespace Microsoft.CodeAnalysis.CodeFixes {
    public struct CodeFixContext {
      public CodeFixContext(Document document, Diagnostic diagnostic, Action<CodeAction, ImmutableArray<Diagnostic>> registerFixregisterCodeFix, CancellationToken cancellationToken);
      public CodeFixContext(Document document, TextSpan span, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction, ImmutableArray<Diagnostic>> registerFixregisterCodeFix, CancellationToken cancellationToken);
+     public void RegisterCodeFix(CodeAction action, Diagnostic diagnostic);
+     public void RegisterCodeFix(CodeAction action, IEnumerable<Diagnostic> diagnostics);
+     public void RegisterCodeFix(CodeAction action, ImmutableArray<Diagnostic> diagnostics);
-     public void RegisterFix(CodeAction action, Diagnostic diagnostic);
-     public void RegisterFix(CodeAction action, IEnumerable<Diagnostic> diagnostics);
-     public void RegisterFix(CodeAction action, ImmutableArray<Diagnostic> diagnostics);
    }
    public abstract class CodeFixProvider {
+     public abstract ImmutableArray<string> FixableDiagnosticIds { get; }
-     public abstract Task ComputeFixesAsync(CodeFixContext context);
-     public abstract ImmutableArray<string> GetFixableDiagnosticIds();
+     public abstract Task RegisterCodeFixesAsync(CodeFixContext context);
    }
    public sealed class ExportCodeFixProviderAttribute : ExportAttribute {
      public ExportCodeFixProviderAttribute(string namefirstLanguage, params string[] languagesadditionalLanguages);
-     public ExportCodeFixProviderAttribute(params string[] languages);
      public string Name { get; set; }
    }
    public class FixAllContext {
+     public string CodeActionEquivalenceKey { get; }
-     public string CodeActionId { get; }
+     public Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(Project project);
-     public Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document);
-     public Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Project project);
+     public Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document);
+     public Task<ImmutableArray<Diagnostic>> GetProjectDiagnosticsAsync(Project project);
    }
  namespace Microsoft.CodeAnalysis.CodeRefactorings {
    public sealed class ExportCodeRefactoringProviderAttribute : ExportAttribute {
-     public ExportCodeRefactoringProviderAttribute(string name, string language);
+     public ExportCodeRefactoringProviderAttribute(string firstLanguage, params string[] additionalLanguages);
-     public string Language { get; }
+     public string[] Languages { get; }
      public string Name { get; set; }
    }
  }
  }
``` 

- Additional documents are represented as `SourceText` instead of streams in the API now as only text files were being handed out as additional documents anyway. AnalyzerOptions has been cleaned up to only have properties which are supported. Also `DiagnosticDescriptor`'s HelpLink was renamed to make it clearer.

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis {
-   public abstract class AdditionalStream {
-     protected AdditionalStream();
-     public abstract string Path { get; }
-     public abstract Stream OpenRead(CancellationToken cancellationToken=null);
    }
+   public abstract class AdditionalText {
+     protected AdditionalText();
+     public abstract string Path { get; }
+     public abstract SourceText GetText(CancellationToken cancellationToken=null);
    }
    public class AnalyzerOptions {
-     public AnalyzerOptions(ImmutableArray<AdditionalStream> additionalStreams, ImmutableDictionary<string, string> globalOptions, CultureInfo culture=null);
+     public AnalyzerOptions(ImmutableArray<AdditionalText> additionalFiles);
+     public ImmutableArray<AdditionalText> AdditionalFiles { get; }
-     public ImmutableArray<AdditionalStream> AdditionalStreams { get; }
-     public CultureInfo Culture { get; }
-     public ImmutableDictionary<string, string> GlobalOptions { get; }
+     public AnalyzerOptions WithAdditionalFiles(ImmutableArray<AdditionalText> additionalFiles);
-     public AnalyzerOptions WithAdditionalStreams(ImmutableArray<AdditionalStream> additionalStreams);
-     public AnalyzerOptions WithCulture(CultureInfo culture);
-     public AnalyzerOptions WithGlobalOptions(ImmutableDictionary<string, string> globalOptions);
    }
    public class DiagnosticDescriptor : IEquatable<DiagnosticDescriptor> {
-     public string HelpLink { get; }
+     public string HelpLinkUri { get; }
+     public bool Equals(DiagnosticDescriptor other);
    }
```

- `AnalyzerDriver` is the type that hosts used to run analyzers and produce diagnostics. However this API leaked a bunch of implementation details and was confusing. The entire type and its friends are all internal now and instead new type called `CompilationWithAnalyzers` has been added for hosts to compute diagnostics from analyzers. As part of this change `SemanticModel.GetDeclarationsInSpan` was removed as well as that method wasn't complete and was very specific to what the AnalyzerDriver needed.

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis.Diagnostics {
+   public class CompilationWithAnalyzers {
+     public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken);
+     public Compilation Compilation { get; }
+     public Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync();
+     public Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync();
+     public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(IEnumerable<Diagnostic> diagnostics, Compilation compilation);
+     public static bool IsDiagnosticAnalyzerSuppressed(DiagnosticAnalyzer analyzer, CompilationOptions options, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException);
    }
+   public static class DiagnosticAnalyzerExtensions {
+     public static CompilationWithAnalyzers WithAnalyzers(this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options=null, CancellationToken cancellationToken=null);
    }

-   public sealed class AnalyzerActions { ... }
-   public abstract class AnalyzerDriver : IDisposable { ... }
-   public class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver { ... }
-   public sealed class AsyncQueue<TElement> { ... }
-   public abstract class CompilationEvent { ... }
-   public sealed class CompilationCompletedEvent : CompilationEvent { ... }
-   public sealed class CompilationStartedEvent : CompilationEvent { ... }
-   public sealed class CompilationUnitCompletedEvent : CompilationEvent { ... }
-   public sealed class SymbolDeclaredCompilationEvent : CompilationEvent { ... }
  }

  namespace Microsoft.CodeAnalysis {
    public abstract class Compilation {
-     public abstract Compilation WithEventQueue(AsyncQueue<CompilationEvent> eventQueue);
    }
    public struct DeclarationInfo {
-     public SyntaxNode DeclaredNode { get; }
-     public ISymbol DeclaredSymbol { get; }
-     public ImmutableArray<SyntaxNode> ExecutableCodeBlocks { get; }
    }
    public abstract class SemanticModel {
-     protected internal abstract ImmutableArray<DeclarationInfo> GetDeclarationsInNode(SyntaxNode node, bool getSymbol, CancellationToken cancellationToken, Nullable<int> levelsToCompute=null);
-     public abstract ImmutableArray<DeclarationInfo> GetDeclarationsInSpan(TextSpan span, bool getSymbol, CancellationToken cancellationToken);
    }  
}
```

- Context objects got public constructors so that they can be unit tested. `RegisterCodeBlockEndAction` became non-generic since the generic parameter wasn't being used.

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis.Diagnostics {
    public abstract class AnalysisContext {
+     protected AnalysisContext();
+     public abstract void RegisterCodeBlockEndAction(Action<CodeBlockEndAnalysisContext> action);
-     public void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct, ValueType;
    }
    public struct CodeBlockEndAnalysisContext {
+     public CodeBlockEndAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public abstract class CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct, ValueType {
+     protected CodeBlockStartAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken);
    }
    public struct CompilationEndAnalysisContext {
+     public CompilationEndAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public abstract class CompilationStartAnalysisContext {
+     protected CompilationStartAnalysisContext(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken);
+     public abstract void RegisterCodeBlockEndAction(Action<CodeBlockEndAnalysisContext> action);
-     public void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct, ValueType;
    }
    public struct SemanticModelAnalysisContext {
+     public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SymbolAnalysisContext {
+     public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SyntaxNodeAnalysisContext {
+     public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SyntaxTreeAnalysisContext {
+     public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
  }
}
```
- **Code Editing**: CTP5 introduced a `SyntaxGenerator` type which made it easy to generate code in fixes or refactorings in a language independent manner. More functionality has been added to that type. Also, all types related to editing code were moved into a new namespace and types like `SyntaxEditor`, `DocumentEditor` and `SolutionEditor` have been introduced to enable writing language independent (between C#\VB) codeactions.

```diff
 assembly Microsoft.CodeAnalysis.Workspaces {
- namespace Microsoft.CodeAnalysis.CodeGeneration {
+ namespace Microsoft.CodeAnalysis.Editing {
+   public class DocumentEditor : SyntaxEditor {
+     public Document OriginalDocument { get; }
+     public SemanticModel SemanticModel { get; }
+     public static Task<DocumentEditor> CreateAsync(Document document, CancellationToken cancellationToken=null);
+     public Document GetChangedDocument();
    }
+   public class SolutionEditor {
+     public SolutionEditor(Solution solution);
+     public Solution OriginalSolution { get; }
+     public Solution GetChangedSolution();
+     public Task<DocumentEditor> GetDocumentEditorAsync(DocumentId id, CancellationToken cancellationToken=null);
    }
+   public sealed class SymbolEditor {
-     public SymbolEditor(Document document);
-     public SymbolEditor(Solution solution);
-     public Solution CurrentSolution { get; }
+     public Solution ChangedSolution { get; }
+     public Solution OriginalSolution { get; }
+     public static SymbolEditor Create(Document document);
+     public static SymbolEditor Create(Solution solution);
-     public Task<ISymbol> EditAllDeclarationsAsync(ISymbol symbol, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditAllDeclarationsAsync(ISymbol symbol, SymbolEditor.AsyncDeclarationEditAction editAction, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditAllDeclarationsAsync(ISymbol symbol, SymbolEditor.DeclarationEditAction editAction, CancellationToken cancellationToken=null);
-     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, SymbolEditor.AsyncDeclarationEditAction editAction, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, SymbolEditor.DeclarationEditAction editAction, CancellationToken cancellationToken=null);
-     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, ISymbol member, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, ISymbol member, SymbolEditor.AsyncDeclarationEditAction editAction, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, ISymbol member, SymbolEditor.DeclarationEditAction editAction, CancellationToken cancellationToken=null);
-     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, Location location, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, Location location, SymbolEditor.AsyncDeclarationEditAction editAction, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, Location location, SymbolEditor.DeclarationEditAction editAction, CancellationToken cancellationToken=null);
+     public IEnumerable<Document> GetChangedDocuments();
+     public Task<IReadOnlyList<SyntaxNode>> GetCurrentDeclarationsAsync(ISymbol symbol, CancellationToken cancellationToken=null);
+     public Task<ISymbol> GetCurrentSymbolAsync(ISymbol symbol, CancellationToken cancellationToken=null);
+     public delegate Task AsyncDeclarationEditAction(DocumentEditor editor, SyntaxNode declaration, CancellationToken cancellationToken);
+     public delegate void DeclarationEditAction(DocumentEditor editor, SyntaxNode declaration);
    }
+   public static class SymbolEditorExtensions {
+     public static Task<SyntaxNode> GetBaseOrInterfaceDeclarationReferenceAsync(this SymbolEditor editor, ISymbol symbol, ITypeSymbol baseOrInterfaceType, CancellationToken cancellationToken=null);
+     public static Task<ISymbol> SetBaseTypeAsync(this SymbolEditor editor, INamedTypeSymbol symbol, ITypeSymbol newBaseType, CancellationToken cancellationToken=null);
+     public static Task<ISymbol> SetBaseTypeAsync(this SymbolEditor editor, INamedTypeSymbol symbol, Func<SyntaxGenerator, SyntaxNode> getNewBaseType, CancellationToken cancellationToken=null);
    }
+   public class SyntaxEditor {
+     public SyntaxEditor(SyntaxNode root, Workspace workspace);
+     public SyntaxGenerator Generator { get; }
+     public SyntaxNode OriginalRoot { get; }
+     public SyntaxNode GetChangedRoot();
+     public void InsertAfter(SyntaxNode node, SyntaxNode newNode);
+     public void InsertAfter(SyntaxNode node, IEnumerable<SyntaxNode> newNodes);
+     public void InsertBefore(SyntaxNode node, SyntaxNode newNode);
+     public void InsertBefore(SyntaxNode node, IEnumerable<SyntaxNode> newNodes);
+     public void RemoveNode(SyntaxNode node);
+     public void ReplaceNode(SyntaxNode node, SyntaxNode newNode);
+     public void ReplaceNode(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> computeReplacement);
+     public void TrackNode(SyntaxNode node);
    }
+   public static class SyntaxEditorExtensions {
+     public static void AddAttribute(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode attribute);
+     public static void AddAttributeArgument(this SyntaxEditor editor, SyntaxNode attributeDeclaration, SyntaxNode attributeArgument);
+     public static void AddBaseType(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode baseType);
+     public static void AddInterfaceType(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode interfaceType);
+     public static void AddMember(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode member);
+     public static void AddParameter(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode parameter);
+     public static void AddReturnAttribute(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode attribute);
+     public static void InsertMembers(this SyntaxEditor editor, SyntaxNode declaration, int index, IEnumerable<SyntaxNode> members);
+     public static void SetAccessibility(this SyntaxEditor editor, SyntaxNode declaration, Accessibility accessibility);
+     public static void SetExpression(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode expression);
+     public static void SetGetAccessorStatements(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> statements);
+     public static void SetModifiers(this SyntaxEditor editor, SyntaxNode declaration, DeclarationModifiers modifiers);
+     public static void SetName(this SyntaxEditor editor, SyntaxNode declaration, string name);
+     public static void SetSetAccessorStatements(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> statements);
+     public static void SetStatements(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<SyntaxNode> statements);
+     public static void SetType(this SyntaxEditor editor, SyntaxNode declaration, SyntaxNode type);
+     public static void SetTypeConstraint(this SyntaxEditor editor, SyntaxNode declaration, string typeParameterName, SpecialTypeConstraintKind kind, IEnumerable<SyntaxNode> types);
+     public static void SetTypeParameters(this SyntaxEditor editor, SyntaxNode declaration, IEnumerable<string> typeParameters);
    }

   public abstract class SyntaxGenerator : ILanguageService {
+     public SyntaxNode AddAttributeArguments(SyntaxNode attributeDeclaration, IEnumerable<SyntaxNode> attributeArguments);
+     public abstract SyntaxNode AddBaseType(SyntaxNode declaration, SyntaxNode baseType);
+     public abstract SyntaxNode AddInterfaceType(SyntaxNode declaration, SyntaxNode interfaceType);
+     public abstract IReadOnlyList<SyntaxNode> GetAttributeArguments(SyntaxNode attributeDeclaration);
+     public abstract IReadOnlyList<SyntaxNode> GetBaseAndInterfaceTypes(SyntaxNode declaration);
+     public abstract SyntaxNode InsertAttributeArguments(SyntaxNode attributeDeclaration, int index, IEnumerable<SyntaxNode> attributeArguments);
+     public virtual SyntaxNode InsertNodesAfter(SyntaxNode root, SyntaxNode node, IEnumerable<SyntaxNode> newDeclarations);
+     public virtual SyntaxNode InsertNodesBefore(SyntaxNode root, SyntaxNode node, IEnumerable<SyntaxNode> newDeclarations);
-     public abstract SyntaxNode RemoveAttributes(SyntaxNode declaration, IEnumerable<SyntaxNode> attributes);
-     public SyntaxNode RemoveMembers(SyntaxNode declaration, params SyntaxNode[] members);
-     public SyntaxNode RemoveMembers(SyntaxNode declaration, IEnumerable<SyntaxNode> members);
-     public SyntaxNode RemoveNamespaceImports(SyntaxNode declaration, params SyntaxNode[] imports);
-     public SyntaxNode RemoveNamespaceImports(SyntaxNode declaration, IEnumerable<SyntaxNode> imports);
-     public SyntaxNode RemoveParameters(SyntaxNode declaration, IEnumerable<SyntaxNode> parameters);
-     public abstract SyntaxNode RemoveReturnAttributes(SyntaxNode declaration, IEnumerable<SyntaxNode> attributes);
+     public virtual SyntaxNode RemoveNode(SyntaxNode root, SyntaxNode node);
+     public SyntaxNode RemoveNodes(SyntaxNode root, IEnumerable<SyntaxNode> declarations);
+     public virtual SyntaxNode ReplaceNode(SyntaxNode root, SyntaxNode node, SyntaxNode newDeclaration);

 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis {
    public abstract class SyntaxNode {
+     public bool Contains(SyntaxNode node);
    }
    public static class SyntaxNodeExtensions {
+     public static TSyntax WithoutTrivia<TSyntax>(this TSyntax syntax) where TSyntax : SyntaxNode;
+     public static TSyntax WithTriviaFrom<TSyntax>(this TSyntax syntax, SyntaxNode node) where TSyntax : SyntaxNode;
    }
    public struct SyntaxToken : IEquatable<SyntaxToken> {
+     public SyntaxToken WithTriviaFrom(SyntaxToken token);
    }
  }
}
```

##Workspace and hosting API Changes

- `CustomWorkspace` has been renamed to `AdhocWorkspace`. The name CustomWorkspace led people to think that this was the type to use to build their own custom workspaces. However this type is simply a quick-and-dirty workspace that's useful for very simple scenarios. To really have a fully functional workspace, one should derive from Workspace.

```diff
 assembly Microsoft.CodeAnalysis.Workspaces {
  namespace Microsoft.CodeAnalysis {
-   public class CustomWorkspace : Workspace {  
+   public sealed class AdhocWorkspace : Workspace {
+     public AdhocWorkspace();
+     public override bool CanOpenDocuments { get; }
+     public override void CloseAdditionalDocument(DocumentId documentId);
+     public override void CloseDocument(DocumentId documentId);
+     public override void OpenAdditionalDocument(DocumentId documentId, bool activate=true);
+     public override void OpenDocument(DocumentId documentId, bool activate=true);
  }
}
```

- Workspace's method names were a bit misleading. A bunch of methods which were simply named with action verbs like `AddDocument` led people to believe those methods would take the action. However those methods are actually called by `ApplyChanges` and that's where an extender would implement the logic to add the document. So all such methods got an "Apply" prefix:

```diff
 assembly Microsoft.CodeAnalysis.Workspaces {
  namespace Microsoft.CodeAnalysis {
    public abstract class Workspace : IDisposable {
-     protected virtual void AddAdditionalDocument(DocumentInfo info, SourceText text);
-     protected virtual void AddAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference);
-     protected virtual void AddDocument(DocumentInfo info, SourceText text);
-     protected virtual void AddMetadataReference(ProjectId projectId, MetadataReference metadataReference);
-     protected virtual void AddProjectReference(ProjectId projectId, ProjectReference projectReference);
+     protected virtual void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text);
+     protected virtual void ApplyAdditionalDocumentRemoved(DocumentId documentId);
+     protected virtual void ApplyAdditionalDocumentTextChanged(DocumentId id, SourceText text);
+     protected virtual void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference);
+     protected virtual void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference);
+     protected virtual void ApplyCompilationOptionsChanged(ProjectId projectId, CompilationOptions options);
+     protected virtual void ApplyDocumentAdded(DocumentInfo info, SourceText text);
+     protected virtual void ApplyDocumentRemoved(DocumentId documentId);
+     protected virtual void ApplyDocumentTextChanged(DocumentId id, SourceText text);
+     protected virtual void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference);
+     protected virtual void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference);
+     protected virtual void ApplyParseOptionsChanged(ProjectId projectId, ParseOptions options);
+     protected virtual void ApplyProjectAdded(ProjectInfo project);
+     protected virtual void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference);
+     protected virtual void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference);
+     protected virtual void ApplyProjectRemoved(ProjectId projectId);
-     protected virtual void ChangedAdditionalDocumentText(DocumentId id, SourceText text);
-     protected virtual void ChangedDocumentText(DocumentId id, SourceText text);
+     protected void CheckCanOpenDocuments();
-     protected virtual void RemoveAdditionalDocument(DocumentId documentId);
-     protected virtual void RemoveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference);
-     protected virtual void RemoveDocument(DocumentId documentId);
-     protected virtual void RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference);
-     protected virtual void RemoveProjectReference(ProjectId projectId, ProjectReference projectReference);
    }
  }
}
```
- Some more cleanup where APIs were either missing a parameter or had a parameter that wasn't doing what users expected

```diff
 assembly Microsoft.CodeAnalysis.Workspaces {
  namespace Microsoft.CodeAnalysis {
    public sealed class DocumentInfo {
+     public static DocumentInfo Create(DocumentId id, string name, IEnumerable<string> folders=null, SourceCodeKind sourceCodeKind=(SourceCodeKind)(0), TextLoader loader=null, string filePath=null, bool isGenerated=false);
-     public static DocumentInfo Create(DocumentId id, string name, IEnumerable<string> folders=null, SourceCodeKind sourceCodeKind=(SourceCodeKind)(0), TextLoader loader=null, string filePath=null, Encoding defaultEncoding=null, bool isGenerated=false);
-     public DocumentInfo WithDefaultEncoding(Encoding encoding);
    }
    public class Project {
-     public TextDocument AddAdditionalDocument(string name, SourceText text, IEnumerable<string> folders=null);
+     public TextDocument AddAdditionalDocument(string name, SourceText text, IEnumerable<string> folders=null, string filePath=null);
-     public TextDocument AddAdditionalDocument(string name, string text, IEnumerable<string> folders=null);
+     public TextDocument AddAdditionalDocument(string name, string text, IEnumerable<string> folders=null, string filePath=null);
-     public Document AddDocument(string name, SyntaxNode syntaxRoot, IEnumerable<string> folders=null);
+     public Document AddDocument(string name, SyntaxNode syntaxRoot, IEnumerable<string> folders=null, string filePath=null);
-     public Document AddDocument(string name, SourceText text, IEnumerable<string> folders=null);
+     public Document AddDocument(string name, SourceText text, IEnumerable<string> folders=null, string filePath=null);
-     public Document AddDocument(string name, string text, IEnumerable<string> folders=null);
+     public Document AddDocument(string name, string text, IEnumerable<string> folders=null, string filePath=null);
    }
  }
}
```

##Changes resulting from language features

- **C#** 
    - Most of the changes were from changes to String Interpolation. 
    - CSharpKind() has been renamed to Kind(). 

```diff
 assembly Microsoft.CodeAnalysis.CSharp {
  namespace Microsoft.CodeAnalysis {
    namespace Microsoft.CodeAnalysis {
    public static class CSharpExtensions {
-     public static SyntaxKind CSharpContextualKind(this SyntaxToken token);
-     public static SyntaxKind CSharpKind(this SyntaxNode node);
-     public static SyntaxKind CSharpKind(this SyntaxNodeOrToken nodeOrToken);
-     public static SyntaxKind CSharpKind(this SyntaxToken token);
-     public static SyntaxKind CSharpKind(this SyntaxTrivia trivia);
-     public static bool IsContextualKind(this SyntaxToken token, SyntaxKind kind);
    }
  }
  namespace Microsoft.CodeAnalysis.CSharp {
    public struct Conversion : IEquatable<Conversion> {
+     public bool IsInterpolatedString { get; }
    }

    public static class CSharpExtensions {
+     public static SyntaxKind Kind(this SyntaxNode node);
+     public static SyntaxKind Kind(this SyntaxNodeOrToken nodeOrToken);
+     public static SyntaxKind Kind(this SyntaxToken token);
+     public static SyntaxKind Kind(this SyntaxTrivia trivia);
    }
    public abstract class CSharpSyntaxNode : SyntaxNode {
-     public SyntaxKind CSharpKind();
+     public SyntaxKind Kind();
    }
    public abstract class CSharpSyntaxRewriter : CSharpSyntaxVisitor<SyntaxNode> {
-     public override SyntaxNode VisitInterpolatedString(InterpolatedStringSyntax node);
+     public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);
-     public override SyntaxNode VisitInterpolatedStringInsert(InterpolatedStringInsertSyntax node);
+     public override SyntaxNode VisitInterpolatedStringText(InterpolatedStringTextSyntax node);
+     public override SyntaxNode VisitInterpolation(InterpolationSyntax node);
+     public override SyntaxNode VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);
+     public override SyntaxNode VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);
    }
    public abstract class CSharpSyntaxVisitor {
-     public virtual void VisitInterpolatedString(InterpolatedStringSyntax node);
+     public virtual void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);
-     public virtual void VisitInterpolatedStringInsert(InterpolatedStringInsertSyntax node);
+     public virtual void VisitInterpolatedStringText(InterpolatedStringTextSyntax node);
+     public virtual void VisitInterpolation(InterpolationSyntax node);
+     public virtual void VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);
+     public virtual void VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);
    }
    public abstract class CSharpSyntaxVisitor<TResult> {
-     public virtual TResult VisitInterpolatedString(InterpolatedStringSyntax node);
+     public virtual TResult VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);
-     public virtual TResult VisitInterpolatedStringInsert(InterpolatedStringInsertSyntax node);
+     public virtual TResult VisitInterpolatedStringText(InterpolatedStringTextSyntax node);
+     public virtual TResult VisitInterpolation(InterpolationSyntax node);
+     public virtual TResult VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);
+     public virtual TResult VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);
    }
    public static class SyntaxFactory {
      public static CatchFilterClauseSyntax CatchFilterClause(SyntaxToken ifKeywordwhenKeyword, SyntaxToken openParenToken, ExpressionSyntax filterExpression, SyntaxToken closeParenToken);
-     public static InterpolatedStringSyntax InterpolatedString(SeparatedSyntaxList<InterpolatedStringInsertSyntax> interpolatedInserts=null);
-     public static InterpolatedStringSyntax InterpolatedString(SyntaxToken stringStart, SeparatedSyntaxList<InterpolatedStringInsertSyntax> interpolatedInserts, SyntaxToken stringEnd);
+     public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken stringStartToken);
+     public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken stringStartToken, SyntaxList<InterpolatedStringContentSyntax> contents);
+     public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken stringStartToken, SyntaxList<InterpolatedStringContentSyntax> contents, SyntaxToken stringEndToken);
-     public static InterpolatedStringInsertSyntax InterpolatedStringInsert(ExpressionSyntax expression);
-     public static InterpolatedStringInsertSyntax InterpolatedStringInsert(ExpressionSyntax expression, SyntaxToken comma, ExpressionSyntax alignment, SyntaxToken format);
+     public static InterpolatedStringTextSyntax InterpolatedStringText();
+     public static InterpolatedStringTextSyntax InterpolatedStringText(SyntaxToken textToken);
+     public static InterpolationSyntax Interpolation(ExpressionSyntax expression);
+     public static InterpolationSyntax Interpolation(ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause);
+     public static InterpolationSyntax Interpolation(SyntaxToken openBraceToken, ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause, SyntaxToken closeBraceToken);
+     public static InterpolationAlignmentClauseSyntax InterpolationAlignmentClause(SyntaxToken commaToken, ExpressionSyntax value);
+     public static InterpolationFormatClauseSyntax InterpolationFormatClause(SyntaxToken colonToken);
+     public static InterpolationFormatClauseSyntax InterpolationFormatClause(SyntaxToken colonToken, SyntaxToken formatStringToken);
    }
    public enum SyntaxKind : ushort {
-     InterpolatedString = (ushort)8655,
      InterpolatedStringEndToken = (ushort)85188483,
+     InterpolatedStringExpression = (ushort)8655,
-     InterpolatedStringInsert = (ushort)8918,
-     InterpolatedStringMidToken = (ushort)8517,
      InterpolatedStringStartToken = (ushort)85168482,
+     InterpolatedStringText = (ushort)8919,
+     InterpolatedStringTextToken = (ushort)8517,
+     InterpolatedVerbatimStringStartToken = (ushort)8484,
+     Interpolation = (ushort)8918,
+     InterpolationAlignmentClause = (ushort)8920,
+     InterpolationFormatClause = (ushort)8921,
+     WhenKeyword = (ushort)8437,
    }
  }
  namespace Microsoft.CodeAnalysis.CSharp.Syntax {
    public sealed class CatchFilterClauseSyntax : CSharpSyntaxNode {
-     public SyntaxToken IfKeyword { get; }
+     public SyntaxToken WhenKeyword { get; }
      public CatchFilterClauseSyntax Update(SyntaxToken ifKeywordwhenKeyword, SyntaxToken openParenToken, ExpressionSyntax filterExpression, SyntaxToken closeParenToken);
-     public CatchFilterClauseSyntax WithIfKeyword(SyntaxToken ifKeyword);
+     public CatchFilterClauseSyntax WithWhenKeyword(SyntaxToken whenKeyword);
    }
+   public abstract class InterpolatedStringContentSyntax : CSharpSyntaxNode {
    }
+   public sealed class InterpolatedStringExpressionSyntax : ExpressionSyntax {
+     public SyntaxList<InterpolatedStringContentSyntax> Contents { get; }
+     public SyntaxToken StringEndToken { get; }
+     public SyntaxToken StringStartToken { get; }
+     public override void Accept(CSharpSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);
+     public InterpolatedStringExpressionSyntax AddContents(params InterpolatedStringContentSyntax[] items);
+     public InterpolatedStringExpressionSyntax Update(SyntaxToken stringStartToken, SyntaxList<InterpolatedStringContentSyntax> contents, SyntaxToken stringEndToken);
+     public InterpolatedStringExpressionSyntax WithContents(SyntaxList<InterpolatedStringContentSyntax> contents);
+     public InterpolatedStringExpressionSyntax WithStringEndToken(SyntaxToken stringEndToken);
+     public InterpolatedStringExpressionSyntax WithStringStartToken(SyntaxToken stringStartToken);
    }
-   public sealed class InterpolatedStringInsertSyntax : CSharpSyntaxNode {
-     public ExpressionSyntax Alignment { get; }
-     public SyntaxToken Comma { get; }
-     public ExpressionSyntax Expression { get; }
-     public SyntaxToken Format { get; }
-     public override void Accept(CSharpSyntaxVisitor visitor);
-     public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);
-     public InterpolatedStringInsertSyntax Update(ExpressionSyntax expression, SyntaxToken comma, ExpressionSyntax alignment, SyntaxToken format);
-     public InterpolatedStringInsertSyntax WithAlignment(ExpressionSyntax alignment);
-     public InterpolatedStringInsertSyntax WithComma(SyntaxToken comma);
-     public InterpolatedStringInsertSyntax WithExpression(ExpressionSyntax expression);
-     public InterpolatedStringInsertSyntax WithFormat(SyntaxToken format);
    }
-   public sealed class InterpolatedStringSyntax : ExpressionSyntax {
-     public SeparatedSyntaxList<InterpolatedStringInsertSyntax> InterpolatedInserts { get; }
-     public SyntaxToken StringEnd { get; }
-     public SyntaxToken StringStart { get; }
-     public override void Accept(CSharpSyntaxVisitor visitor);
-     public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);
-     public InterpolatedStringSyntax AddInterpolatedInserts(params InterpolatedStringInsertSyntax[] items);
-     public InterpolatedStringSyntax Update(SyntaxToken stringStart, SeparatedSyntaxList<InterpolatedStringInsertSyntax> interpolatedInserts, SyntaxToken stringEnd);
-     public InterpolatedStringSyntax WithInterpolatedInserts(SeparatedSyntaxList<InterpolatedStringInsertSyntax> interpolatedInserts);
-     public InterpolatedStringSyntax WithStringEnd(SyntaxToken stringEnd);
-     public InterpolatedStringSyntax WithStringStart(SyntaxToken stringStart);
    }
+   public sealed class InterpolatedStringTextSyntax : InterpolatedStringContentSyntax {
+     public SyntaxToken TextToken { get; }
+     public override void Accept(CSharpSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);
+     public InterpolatedStringTextSyntax Update(SyntaxToken textToken);
+     public InterpolatedStringTextSyntax WithTextToken(SyntaxToken textToken);
    }
+   public sealed class InterpolationAlignmentClauseSyntax : CSharpSyntaxNode {
+     public SyntaxToken CommaToken { get; }
+     public ExpressionSyntax Value { get; }
+     public override void Accept(CSharpSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);
+     public InterpolationAlignmentClauseSyntax Update(SyntaxToken commaToken, ExpressionSyntax value);
+     public InterpolationAlignmentClauseSyntax WithCommaToken(SyntaxToken commaToken);
+     public InterpolationAlignmentClauseSyntax WithValue(ExpressionSyntax value);
    }
+   public sealed class InterpolationFormatClauseSyntax : CSharpSyntaxNode {
+     public SyntaxToken ColonToken { get; }
+     public SyntaxToken FormatStringToken { get; }
+     public override void Accept(CSharpSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);
+     public InterpolationFormatClauseSyntax Update(SyntaxToken colonToken, SyntaxToken formatStringToken);
+     public InterpolationFormatClauseSyntax WithColonToken(SyntaxToken colonToken);
+     public InterpolationFormatClauseSyntax WithFormatStringToken(SyntaxToken formatStringToken);
    }
+   public sealed class InterpolationSyntax : InterpolatedStringContentSyntax {
+     public InterpolationAlignmentClauseSyntax AlignmentClause { get; }
+     public SyntaxToken CloseBraceToken { get; }
+     public ExpressionSyntax Expression { get; }
+     public InterpolationFormatClauseSyntax FormatClause { get; }
+     public SyntaxToken OpenBraceToken { get; }
+     public override void Accept(CSharpSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);
+     public InterpolationSyntax Update(SyntaxToken openBraceToken, ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause, SyntaxToken closeBraceToken);
+     public InterpolationSyntax WithAlignmentClause(InterpolationAlignmentClauseSyntax alignmentClause);
+     public InterpolationSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);
+     public InterpolationSyntax WithExpression(ExpressionSyntax expression);
+     public InterpolationSyntax WithFormatClause(InterpolationFormatClauseSyntax formatClause);
+     public InterpolationSyntax WithOpenBraceToken(SyntaxToken openBraceToken);
    }
  }
 }
 ```

- **VB**
  - String Interpolation work resulted in a bunch of new syntax APIs.
  - VBKind() has been renamed to Kind()
  - Syntax nodes for block declarations (e.g. ClassBlockSyntax, ConstructorBlockSyntax) have been changed to have block specific child names (e.g. SubNewStatement, EndSubStatement) instead of generic ones (e.g. Begin/End). This was done to make these members consistent with the rest of the API.
  - Syntax nodes for various statements (e.g. MethodStatementSyntax) have been changed to have statement specific child names (e.g. SubOrFunctionKeyword) instead of generic ones (e.g. Keyword). This was done to make these members consistent with the rest of the API.

```diff
 assembly Microsoft.CodeAnalysis.VisualBasic {
  namespace Microsoft.CodeAnalysis {
    public sealed class VisualBasicExtensions {
-     public static bool IsContextualKind(this SyntaxToken token, SyntaxKind kind);
-     public static SyntaxKind VBKind(this SyntaxNode node);
-     public static SyntaxKind VBKind(this SyntaxNodeOrToken nodeOrToken);
-     public static SyntaxKind VBKind(this SyntaxToken token);
-     public static SyntaxKind VBKind(this SyntaxTrivia trivia);
-     public static SyntaxKind VisualBasicContextualKind(this SyntaxToken token);
    }
  }
  namespace Microsoft.CodeAnalysis.VisualBasic {
    public class SyntaxFactory {
+     public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxList<InterpolatedStringContentSyntax> contents);
+     public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(SyntaxToken dollarSignDoubleQuoteToken, SyntaxList<InterpolatedStringContentSyntax> contents, SyntaxToken doubleQuoteToken);
+     public static InterpolatedStringExpressionSyntax InterpolatedStringExpression(params InterpolatedStringContentSyntax[] contents);
+     public static InterpolatedStringTextSyntax InterpolatedStringText();
+     public static InterpolatedStringTextSyntax InterpolatedStringText(SyntaxToken textToken);
+     public static SyntaxToken InterpolatedStringTextToken(SyntaxTriviaList leadingTrivia, string text, string value, SyntaxTriviaList trailingTrivia);
+     public static SyntaxToken InterpolatedStringTextToken(string text, string value);
+     public static InterpolationSyntax Interpolation(SyntaxToken openBraceToken, ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause, SyntaxToken closeBraceToken);
+     public static InterpolationSyntax Interpolation(ExpressionSyntax expression);
+     public static InterpolationSyntax Interpolation(ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause);
+     public static InterpolationAlignmentClauseSyntax InterpolationAlignmentClause(SyntaxToken commaToken, ExpressionSyntax value);
+     public static InterpolationAlignmentClauseSyntax InterpolationAlignmentClause(ExpressionSyntax value);
+     public static InterpolationFormatClauseSyntax InterpolationFormatClause();
+     public static InterpolationFormatClauseSyntax InterpolationFormatClause(SyntaxToken colonToken, SyntaxToken formatStringToken);
    }
    public class SyntaxFacts {
+     public static bool IsAccessorStatementAccessorKeyword(SyntaxKind kind);
+     public static bool IsDeclareStatementSubOrFunctionKeyword(SyntaxKind kind);
+     public static bool IsDelegateStatementSubOrFunctionKeyword(SyntaxKind kind);
+     public static bool IsLambdaHeaderSubOrFunctionKeyword(SyntaxKind kind);
+     public static bool IsMethodStatementSubOrFunctionKeyword(SyntaxKind kind);
    }
    public enum SyntaxKind : ushort {
+     DollarSignDoubleQuoteToken = (ushort)785,
+     EndOfInterpolatedStringToken = (ushort)787,
+     InterpolatedStringExpression = (ushort)780,
+     InterpolatedStringText = (ushort)781,
+     InterpolatedStringTextToken = (ushort)786,
+     Interpolation = (ushort)782,
+     InterpolationAlignmentClause = (ushort)783,
+     InterpolationFormatClause = (ushort)784,
    }
    public sealed class VisualBasicExtensions {
+     public static SyntaxKind Kind(this SyntaxNode node);
+     public static SyntaxKind Kind(this SyntaxNodeOrToken nodeOrToken);
+     public static SyntaxKind Kind(this SyntaxToken token);
+     public static SyntaxKind Kind(this SyntaxTrivia trivia);
    }
    public abstract class VisualBasicSyntaxNode : SyntaxNode {
+     public SyntaxKind Kind();
-     public SyntaxKind VBKind();
    }
    public abstract class VisualBasicSyntaxRewriter : VisualBasicSyntaxVisitor<SyntaxNode> {
+     public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);
+     public override SyntaxNode VisitInterpolatedStringText(InterpolatedStringTextSyntax node);
+     public override SyntaxNode VisitInterpolation(InterpolationSyntax node);
+     public override SyntaxNode VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);
+     public override SyntaxNode VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);
    }
    public abstract class VisualBasicSyntaxVisitor {
+     public virtual void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);
+     public virtual void VisitInterpolatedStringText(InterpolatedStringTextSyntax node);
+     public virtual void VisitInterpolation(InterpolationSyntax node);
+     public virtual void VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);
+     public virtual void VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);
    }
    public abstract class VisualBasicSyntaxVisitor<TResult> {
+     public virtual TResult VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node);
+     public virtual TResult VisitInterpolatedStringText(InterpolatedStringTextSyntax node);
+     public virtual TResult VisitInterpolation(InterpolationSyntax node);
+     public virtual TResult VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node);
+     public virtual TResult VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node);
    }
  }
  namespace Microsoft.CodeAnalysis.VisualBasic.Syntax {
    public sealed class AccessorBlockSyntax : MethodBlockBaseSyntax {
+     public AccessorStatementSyntax AccessorStatement { get; }
+     public override MethodBaseSyntax BlockStatement { get; }
+     public EndBlockStatementSyntax EndAccessorStatement { get; }
+     public override EndBlockStatementSyntax EndBlockStatement { get; }
+     public AccessorBlockSyntax WithAccessorStatement(AccessorStatementSyntax accessorStatement);
+     public override MethodBlockBaseSyntax WithBlockStatement(MethodBaseSyntax blockStatement);
+     public AccessorBlockSyntax WithEndAccessorStatement(EndBlockStatementSyntax endAccessorStatement);
+     public override MethodBlockBaseSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
    }
    public sealed class AccessorStatementSyntax : MethodBaseSyntax {
+     public SyntaxToken AccessorKeyword { get; }
+     public override SyntaxToken DeclarationKeyword { get; }
+     public AccessorStatementSyntax WithAccessorKeyword(SyntaxToken accessorKeyword);
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
    }
    public sealed class CaseBlockSyntax : VisualBasicSyntaxNode {
+     public CaseStatementSyntax CaseStatement { get; }
+     public CaseBlockSyntax AddCaseStatementCases(params CaseClauseSyntax[] items);
+     public CaseBlockSyntax WithCaseStatement(CaseStatementSyntax caseStatement);
    }
    public sealed class ClassBlockSyntax : TypeBlockSyntax {
+     public override TypeStatementSyntax BlockStatement { get; }
+     public ClassStatementSyntax ClassStatement { get; }
+     public override EndBlockStatementSyntax EndBlockStatement { get; }
+     public EndBlockStatementSyntax EndClassStatement { get; }
+     public override TypeBlockSyntax WithBlockStatement(TypeStatementSyntax blockStatement);
+     public ClassBlockSyntax WithClassStatement(ClassStatementSyntax classStatement);
+     public override TypeBlockSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
+     public ClassBlockSyntax WithEndClassStatement(EndBlockStatementSyntax endClassStatement);
    }
    public sealed class ClassStatementSyntax : TypeStatementSyntax {
+     public SyntaxToken ClassKeyword { get; }
+     public override SyntaxToken DeclarationKeyword { get; }
+     public ClassStatementSyntax WithClassKeyword(SyntaxToken classKeyword);
+     public override TypeStatementSyntax WithDeclarationKeyword(SyntaxToken keyword);
    }
    public sealed class ConstructorBlockSyntax : MethodBlockBaseSyntax {
+     public override MethodBaseSyntax BlockStatement { get; }
+     public override EndBlockStatementSyntax EndBlockStatement { get; }
+     public EndBlockStatementSyntax EndSubStatement { get; }
+     public SubNewStatementSyntax SubNewStatement { get; }
+     public override MethodBlockBaseSyntax WithBlockStatement(MethodBaseSyntax blockStatement);
+     public override MethodBlockBaseSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
+     public ConstructorBlockSyntax WithEndSubStatement(EndBlockStatementSyntax endSubStatement);
+     public ConstructorBlockSyntax WithSubNewStatement(SubNewStatementSyntax subNewStatement);
    }
    public sealed class CrefOperatorReferenceSyntax : NameSyntax {
+     public SyntaxToken OperatorKeyword { get; }
+     public CrefOperatorReferenceSyntax WithOperatorKeyword(SyntaxToken operatorKeyword);
    }
    public sealed class DeclareStatementSyntax : MethodBaseSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken SubOrFunctionKeyword { get; }
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public DeclareStatementSyntax WithSubOrFunctionKeyword(SyntaxToken subOrFunctionKeyword);
    }
    public sealed class DelegateStatementSyntax : MethodBaseSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken SubOrFunctionKeyword { get; }
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public DelegateStatementSyntax WithSubOrFunctionKeyword(SyntaxToken subOrFunctionKeyword);
    }
    public sealed class EventStatementSyntax : MethodBaseSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken EventKeyword { get; }
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public EventStatementSyntax WithEventKeyword(SyntaxToken eventKeyword);
    }
    public sealed class InterfaceBlockSyntax : TypeBlockSyntax {
+     public override TypeStatementSyntax BlockStatement { get; }
+     public override EndBlockStatementSyntax EndBlockStatement { get; }
+     public EndBlockStatementSyntax EndInterfaceStatement { get; }
+     public InterfaceStatementSyntax InterfaceStatement { get; }
+     public override TypeBlockSyntax WithBlockStatement(TypeStatementSyntax blockStatement);
+     public override TypeBlockSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
+     public InterfaceBlockSyntax WithEndInterfaceStatement(EndBlockStatementSyntax endInterfaceStatement);
+     public InterfaceBlockSyntax WithInterfaceStatement(InterfaceStatementSyntax interfaceStatement);
    }
    public sealed class InterfaceStatementSyntax : TypeStatementSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken InterfaceKeyword { get; }
+     public override TypeStatementSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public InterfaceStatementSyntax WithInterfaceKeyword(SyntaxToken interfaceKeyword);
    }
+   public abstract class InterpolatedStringContentSyntax : VisualBasicSyntaxNode {
    }
+   public sealed class InterpolatedStringExpressionSyntax : ExpressionSyntax {
+     public SyntaxList<InterpolatedStringContentSyntax> Contents { get; }
+     public SyntaxToken DollarSignDoubleQuoteToken { get; }
+     public SyntaxToken DoubleQuoteToken { get; }
+     public override void Accept(VisualBasicSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(VisualBasicSyntaxVisitor<TResult> visitor);
+     public InterpolatedStringExpressionSyntax AddContents(params InterpolatedStringContentSyntax[] items);
+     public InterpolatedStringExpressionSyntax Update(SyntaxToken dollarSignDoubleQuoteToken, SyntaxList<InterpolatedStringContentSyntax> contents, SyntaxToken doubleQuoteToken);
+     public InterpolatedStringExpressionSyntax WithContents(SyntaxList<InterpolatedStringContentSyntax> contents);
+     public InterpolatedStringExpressionSyntax WithDollarSignDoubleQuoteToken(SyntaxToken dollarSignDoubleQuoteToken);
+     public InterpolatedStringExpressionSyntax WithDoubleQuoteToken(SyntaxToken doubleQuoteToken);
    }
+   public sealed class InterpolatedStringTextSyntax : InterpolatedStringContentSyntax {
+     public SyntaxToken TextToken { get; }
+     public override void Accept(VisualBasicSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(VisualBasicSyntaxVisitor<TResult> visitor);
+     public InterpolatedStringTextSyntax Update(SyntaxToken textToken);
+     public InterpolatedStringTextSyntax WithTextToken(SyntaxToken textToken);
    }
+   public sealed class InterpolationAlignmentClauseSyntax : VisualBasicSyntaxNode {
+     public SyntaxToken CommaToken { get; }
+     public ExpressionSyntax Value { get; }
+     public override void Accept(VisualBasicSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(VisualBasicSyntaxVisitor<TResult> visitor);
+     public InterpolationAlignmentClauseSyntax Update(SyntaxToken commaToken, ExpressionSyntax value);
+     public InterpolationAlignmentClauseSyntax WithCommaToken(SyntaxToken commaToken);
+     public InterpolationAlignmentClauseSyntax WithValue(ExpressionSyntax value);
    }
+   public sealed class InterpolationFormatClauseSyntax : VisualBasicSyntaxNode {
+     public SyntaxToken ColonToken { get; }
+     public SyntaxToken FormatStringToken { get; }
+     public override void Accept(VisualBasicSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(VisualBasicSyntaxVisitor<TResult> visitor);
+     public InterpolationFormatClauseSyntax Update(SyntaxToken colonToken, SyntaxToken formatStringToken);
+     public InterpolationFormatClauseSyntax WithColonToken(SyntaxToken colonToken);
+     public InterpolationFormatClauseSyntax WithFormatStringToken(SyntaxToken formatStringToken);
    }
+   public sealed class InterpolationSyntax : InterpolatedStringContentSyntax {
+     public InterpolationAlignmentClauseSyntax AlignmentClause { get; }
+     public SyntaxToken CloseBraceToken { get; }
+     public ExpressionSyntax Expression { get; }
+     public InterpolationFormatClauseSyntax FormatClause { get; }
+     public SyntaxToken OpenBraceToken { get; }
+     public override void Accept(VisualBasicSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(VisualBasicSyntaxVisitor<TResult> visitor);
+     public InterpolationSyntax Update(SyntaxToken openBraceToken, ExpressionSyntax expression, InterpolationAlignmentClauseSyntax alignmentClause, InterpolationFormatClauseSyntax formatClause, SyntaxToken closeBraceToken);
+     public InterpolationSyntax WithAlignmentClause(InterpolationAlignmentClauseSyntax alignmentClause);
+     public InterpolationSyntax WithCloseBraceToken(SyntaxToken closeBraceToken);
+     public InterpolationSyntax WithExpression(ExpressionSyntax expression);
+     public InterpolationSyntax WithFormatClause(InterpolationFormatClauseSyntax formatClause);
+     public InterpolationSyntax WithOpenBraceToken(SyntaxToken openBraceToken);
    }
    public abstract class LambdaExpressionSyntax : ExpressionSyntax {
+     public LambdaHeaderSyntax SubOrFunctionHeader { get; }
    }
    public sealed class LambdaHeaderSyntax : MethodBaseSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken SubOrFunctionKeyword { get; }
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public LambdaHeaderSyntax WithSubOrFunctionKeyword(SyntaxToken subOrFunctionKeyword);
    }
    public abstract class MethodBaseSyntax : DeclarationStatementSyntax {
+     public abstract SyntaxToken DeclarationKeyword { get; }
+     public abstract MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public MethodBaseSyntax WithKeyword(SyntaxToken keyword);
    }
    public abstract class MethodBlockBaseSyntax : DeclarationStatementSyntax {
+     public abstract MethodBaseSyntax BlockStatement { get; }
+     public abstract EndBlockStatementSyntax EndBlockStatement { get; }
+     public MethodBlockBaseSyntax WithBegin(MethodBaseSyntax begin);
+     public abstract MethodBlockBaseSyntax WithBlockStatement(MethodBaseSyntax blockStatement);
+     public MethodBlockBaseSyntax WithEnd(EndBlockStatementSyntax end);
+     public abstract MethodBlockBaseSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
    }
    public sealed class MethodBlockSyntax : MethodBlockBaseSyntax {
+     public override MethodBaseSyntax BlockStatement { get; }
+     public override EndBlockStatementSyntax EndBlockStatement { get; }
+     public EndBlockStatementSyntax EndSubOrFunctionStatement { get; }
+     public MethodStatementSyntax SubOrFunctionStatement { get; }
+     public override MethodBlockBaseSyntax WithBlockStatement(MethodBaseSyntax blockStatement);
+     public override MethodBlockBaseSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
+     public MethodBlockSyntax WithEndSubOrFunctionStatement(EndBlockStatementSyntax endSubOrFunctionStatement);
+     public MethodBlockSyntax WithSubOrFunctionStatement(MethodStatementSyntax subOrFunctionStatement);
    }
    public sealed class MethodStatementSyntax : MethodBaseSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken SubOrFunctionKeyword { get; }
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public MethodStatementSyntax WithSubOrFunctionKeyword(SyntaxToken subOrFunctionKeyword);
    }
    public sealed class ModuleBlockSyntax : TypeBlockSyntax {
+     public override TypeStatementSyntax BlockStatement { get; }
+     public override EndBlockStatementSyntax EndBlockStatement { get; }
+     public EndBlockStatementSyntax EndModuleStatement { get; }
+     public ModuleStatementSyntax ModuleStatement { get; }
+     public override TypeBlockSyntax WithBlockStatement(TypeStatementSyntax blockStatement);
+     public override TypeBlockSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
+     public ModuleBlockSyntax WithEndModuleStatement(EndBlockStatementSyntax endModuleStatement);
+     public ModuleBlockSyntax WithModuleStatement(ModuleStatementSyntax moduleStatement);
    }
    public sealed class ModuleStatementSyntax : TypeStatementSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken ModuleKeyword { get; }
+     public override TypeStatementSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public ModuleStatementSyntax WithModuleKeyword(SyntaxToken moduleKeyword);
    }
    public sealed class MultiLineLambdaExpressionSyntax : LambdaExpressionSyntax {
+     public EndBlockStatementSyntax EndSubOrFunctionStatement { get; }
+     public new LambdaHeaderSyntax SubOrFunctionHeader { get; }
+     public MultiLineLambdaExpressionSyntax WithEndSubOrFunctionStatement(EndBlockStatementSyntax endSubOrFunctionStatement);
+     public MultiLineLambdaExpressionSyntax WithSubOrFunctionHeader(LambdaHeaderSyntax subOrFunctionHeader);
    }
    public sealed class OperatorBlockSyntax : MethodBlockBaseSyntax {
+     public override MethodBaseSyntax BlockStatement { get; }
+     public override EndBlockStatementSyntax EndBlockStatement { get; }
+     public EndBlockStatementSyntax EndOperatorStatement { get; }
+     public OperatorStatementSyntax OperatorStatement { get; }
+     public override MethodBlockBaseSyntax WithBlockStatement(MethodBaseSyntax blockStatement);
+     public override MethodBlockBaseSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
+     public OperatorBlockSyntax WithEndOperatorStatement(EndBlockStatementSyntax endOperatorStatement);
+     public OperatorBlockSyntax WithOperatorStatement(OperatorStatementSyntax operatorStatement);
    }
    public sealed class OperatorStatementSyntax : MethodBaseSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken OperatorKeyword { get; }
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public OperatorStatementSyntax WithOperatorKeyword(SyntaxToken operatorKeyword);
    }
    public sealed class PropertyStatementSyntax : MethodBaseSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken PropertyKeyword { get; }
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public PropertyStatementSyntax WithPropertyKeyword(SyntaxToken propertyKeyword);
    }
    public sealed class SingleLineLambdaExpressionSyntax : LambdaExpressionSyntax {
+     public new LambdaHeaderSyntax SubOrFunctionHeader { get; }
      public SingleLineLambdaExpressionSyntax Update(SyntaxKind kind, LambdaHeaderSyntax beginsubOrFunctionHeader, VisualBasicSyntaxNode body);
+     public SingleLineLambdaExpressionSyntax WithSubOrFunctionHeader(LambdaHeaderSyntax subOrFunctionHeader);
    }
    public sealed class StructureBlockSyntax : TypeBlockSyntax {
+     public override TypeStatementSyntax BlockStatement { get; }
+     public override EndBlockStatementSyntax EndBlockStatement { get; }
+     public EndBlockStatementSyntax EndStructureStatement { get; }
+     public StructureStatementSyntax StructureStatement { get; }
+     public override TypeBlockSyntax WithBlockStatement(TypeStatementSyntax blockStatement);
+     public override TypeBlockSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
+     public StructureBlockSyntax WithEndStructureStatement(EndBlockStatementSyntax endStructureStatement);
+     public StructureBlockSyntax WithStructureStatement(StructureStatementSyntax structureStatement);
    }
    public sealed class StructureStatementSyntax : TypeStatementSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken StructureKeyword { get; }
+     public override TypeStatementSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public StructureStatementSyntax WithStructureKeyword(SyntaxToken structureKeyword);
    }
    public sealed class SubNewStatementSyntax : MethodBaseSyntax {
+     public override SyntaxToken DeclarationKeyword { get; }
+     public SyntaxToken SubKeyword { get; }
+     public override MethodBaseSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public SubNewStatementSyntax WithSubKeyword(SyntaxToken subKeyword);
    }
    public abstract class TypeBlockSyntax : DeclarationStatementSyntax {
+     public abstract TypeStatementSyntax BlockStatement { get; }
+     public abstract EndBlockStatementSyntax EndBlockStatement { get; }
+     public TypeBlockSyntax WithBegin(TypeStatementSyntax begin);
+     public abstract TypeBlockSyntax WithBlockStatement(TypeStatementSyntax blockStatement);
+     public TypeBlockSyntax WithEnd(EndBlockStatementSyntax end);
+     public abstract TypeBlockSyntax WithEndBlockStatement(EndBlockStatementSyntax endBlockStatement);
    }
    public abstract class TypeStatementSyntax : DeclarationStatementSyntax {
+     public abstract SyntaxToken DeclarationKeyword { get; }
+     public abstract TypeStatementSyntax WithDeclarationKeyword(SyntaxToken keyword);
+     public TypeStatementSyntax WithKeyword(SyntaxToken keyword);
    }
  }
 }
```

## Miscellaneous

APIs that weren't meant to be public or that were already deprecated have been removed.

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis {

-   public enum CommonMemberResolutionKind {
-     Applicable = 0,
-     TypeInferenceFailed = 2,
-     UseSiteError = 1,
-     Worse = 3,
    }
    public struct CommonMemberResolutionResult<TMember> where TMember : ISymbol {
-     public bool IsValid { get; }
-     public TMember Member { get; }
-     public CommonMemberResolutionKind Resolution { get; }
    }
    public struct CommonOverloadResolutionResult<TMember> where TMember : ISymbol {
-     public Nullable<CommonMemberResolutionResult<TMember>> BestResult { get; }
-     public ImmutableArray<CommonMemberResolutionResult<TMember>> Results { get; }
-     public bool Succeeded { get; }
-     public Nullable<CommonMemberResolutionResult<TMember>> ValidResult { get; }
    }
-   public static class ImmutableArrayExtensions {
-     public static void AddRange<T, U>(this List<T> list, ImmutableArray<U> items) where U : T;
-     public static ImmutableArray<T> AsImmutable<T>(this IEnumerable<T> items);
-     public static ImmutableArray<T> AsImmutable<T>(this T[] items);
-     public static ImmutableArray<T> AsImmutableOrEmpty<T>(this IEnumerable<T> items);
-     public static ImmutableArray<T> AsImmutableOrEmpty<T>(this T[] items);
-     public static ImmutableArray<T> AsImmutableOrNull<T>(this IEnumerable<T> items);
-     public static ImmutableArray<T> AsImmutableOrNull<T>(this T[] items);
-     [MethodImpl(AggressiveInlining)]public static ImmutableArray<TBase> Cast<TDerived, TBase>(this ImmutableArray<TDerived> items) where TDerived : class, TBase;
-     public static ImmutableArray<T> Distinct<T>(this ImmutableArray<T> array, IEqualityComparer<T> comparer=null);
-     public static ImmutableArray<T> NullToEmpty<T>(this ImmutableArray<T> array);
-     public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ImmutableArray<TItem> items, Func<TItem, int, TArg, TResult> map, TArg arg);
-     public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ImmutableArray<TItem> items, Func<TItem, TArg, TResult> map, TArg arg);
-     public static ImmutableArray<TResult> SelectAsArray<TItem, TResult>(this ImmutableArray<TItem> items, Func<TItem, TResult> map);
-     public static bool SetEquals<T>(this ImmutableArray<T> array1, ImmutableArray<T> array2, IEqualityComparer<T> comparer);
-     public static ImmutableArray<byte> ToImmutable(this MemoryStream stream);
-     public static ImmutableArray<T> WhereAsArray<T>(this ImmutableArray<T> array, Func<T, bool> predicate);
    }
    public enum TypeKind : byte {
-     ArrayType = (byte)1,
-     DynamicType = (byte)4,
-     PointerType = (byte)9,
    }
  }

 assembly Microsoft.CodeAnalysis.Workspaces.Desktop {
  namespace Microsoft.CodeAnalysis.Host.Mef {
    public class MefV1HostServices : HostServices {
-     public static MefV1HostServices DefaultHost { get; }
    }
  }
 }
```
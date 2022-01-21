#API changes between VS 2015 CTP6 and VS 2015 RC

##Diagnostics API Changes

- We've made a couple of changes to the core DiagnosticAnalyzer APIs: 
    - An *EndAction (`CompilationEndAction` or `CodeBlockEndAction`) can now only be registered from within a *Start context. This eliminates the confusion around being able to register an end action without a start action. 
    - Two new actions have been introduced - `CompilationAction` and `CodeBlockAction`. These are the "stateless" equivalents of the Start\End pair. One would register these actions when they simply need to inspect a compilation or a codeblock without needing any accumulated state. They would instead use the Start\End pair if they need to build up some computational state to report diagnostics after having looked at the entire compilation or codeblock. 
- A complete description of the semantics of all the actions is available [here](https://github.com/dotnet/roslyn/tree/main/docs/analyzers/Analyzer%20Actions%20Semantics.md)
- There is now a property bag on the `Diagnostic` type that can be used to communicate information from an analyzer to some other consumer. For eg. a codefixer for that diagnostic might be able to reuse some compuation from the analysis phase to produce a fix.
- Some miscellaneous tightening of the APIs like making types sealed, making the `LocalizableString` type exception-safe, ensuring context.ReportDiagnostic filters out diagnostics not reported by the analyzer as supported. 

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis.Diagnostics {
    public abstract class AnalysisContext {
+     public abstract void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action);
-     public abstract void RegisterCodeBlockEndAction(Action<CodeBlockEndAnalysisContext> action);
+     public abstract void RegisterCompilationAction(Action<CompilationAnalysisContext> action);
-     public abstract void RegisterCompilationEndAction(Action<CompilationEndAnalysisContext> action);
    }
    public class AnalyzerOptions {
+     public override bool Equals(object obj);
+     public override int GetHashCode();
    }
    public struct CodeBlockAnalysisContext {
+     public CodeBlockAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);
+     public CancellationToken CancellationToken { get; }
+     public SyntaxNode CodeBlock { get; }
+     public AnalyzerOptions Options { get; }
+     public ISymbol OwningSymbol { get; }
+     public SemanticModel SemanticModel { get; }
+     public void ReportDiagnostic(Diagnostic diagnostic);
    }
-   public struct CodeBlockEndAnalysisContext {
-     public CodeBlockEndAnalysisContext(SyntaxNode codeBlock, ISymbol owningSymbol, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
-     public CancellationToken CancellationToken { get; }
-     public SyntaxNode CodeBlock { get; }
-     public AnalyzerOptions Options { get; }
-     public ISymbol OwningSymbol { get; }
-     public SemanticModel SemanticModel { get; }
-     public void ReportDiagnostic(Diagnostic diagnostic);
-   }
    public abstract class CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct, ValueType {
+     public abstract void RegisterCodeBlockEndAction(Action<CodeBlockAnalysisContext> action);
-     public abstract void RegisterCodeBlockEndAction(Action<CodeBlockEndAnalysisContext> action);
    }
    public struct CompilationAnalysisContext {
+     public CompilationAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);
+     public CancellationToken CancellationToken { get; }
+     public Compilation Compilation { get; }
+     public AnalyzerOptions Options { get; }
+     public void ReportDiagnostic(Diagnostic diagnostic);
    }
-   public struct CompilationEndAnalysisContext {
-     public CompilationEndAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
-     public CancellationToken CancellationToken { get; }
-     public Compilation Compilation { get; }
-     public AnalyzerOptions Options { get; }
-     public void ReportDiagnostic(Diagnostic diagnostic);
-   }
    public abstract class CompilationStartAnalysisContext {
+     public abstract void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action);
-     public abstract void RegisterCodeBlockEndAction(Action<CodeBlockEndAnalysisContext> action);
+     public abstract void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action);
-     public abstract void RegisterCompilationEndAction(Action<CompilationEndAnalysisContext> action);
    }
    public class CompilationWithAnalyzers {
+     public static bool IsDiagnosticAnalyzerSuppressed(DiagnosticAnalyzer analyzer, CompilationOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException=null);
-     public static bool IsDiagnosticAnalyzerSuppressed(DiagnosticAnalyzer analyzer, CompilationOptions options, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException);
    }
    public abstract class DiagnosticAnalyzer {
+     public sealed override bool Equals(object obj);
+     public sealed override int GetHashCode();
+     public sealed override string ToString();
    }
    public struct SemanticModelAnalysisContext {
+     public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);
-     public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SymbolAnalysisContext {
+     public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);
-     public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SyntaxNodeAnalysisContext {
+     public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);
-     public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SyntaxTreeAnalysisContext {
+     public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, Func<Diagnostic, bool> isSupportedDiagnostic, CancellationToken cancellationToken);
-     public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
  }

 namespace Microsoft.CodeAnalysis {
  public abstract class Diagnostic : IEquatable<Diagnostic>, IFormattable {
+     public virtual ImmutableDictionary<string, string> Properties { get; }
+     public static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, IEnumerable<Location> additionalLocations, ImmutableDictionary<string, string> properties, params object[] messageArgs);
+     public static Diagnostic Create(DiagnosticDescriptor descriptor, Location location, ImmutableDictionary<string, string> properties, params object[] messageArgs);
-     public static Diagnostic Create(string id, string category, LocalizableString message, DiagnosticSeverity severity, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, int warningLevel, LocalizableString title=null, LocalizableString description=null, string helpLink=null, Location location=null, IEnumerable<Location> additionalLocations=null, IEnumerable<string> customTags=null);
+     public static Diagnostic Create(string id, string category, LocalizableString message, DiagnosticSeverity severity, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, int warningLevel, LocalizableString title=null, LocalizableString description=null, string helpLink=null, Location location=null, IEnumerable<Location> additionalLocations=null, IEnumerable<string> customTags=null, ImmutableDictionary<string, string> properties=null);
    }
-   public class DiagnosticDescriptor : IEquatable<DiagnosticDescriptor> {    
+   public sealed class DiagnosticDescriptor : IEquatable<DiagnosticDescriptor> {
    }
    public sealed class LocalizableResourceString : LocalizableString {
+     public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource);
+     protected override bool AreEqual(object other);
+     protected override int GetHash();
+     protected override string GetText(IFormatProvider formatProvider);
-     public override string ToString(IFormatProvider formatProvider);
    }
    public abstract class LocalizableString : IEquatable<LocalizableString>, IFormattable {
+     public event EventHandler<Exception> OnException;
+     protected abstract bool AreEqual(object other);
+     public bool Equals(LocalizableString other);
+     public sealed override bool Equals(object other);
+     protected abstract int GetHash();
+     public sealed override int GetHashCode();
+     protected abstract string GetText(IFormatProvider formatProvider);
-     public abstract string ToString(IFormatProvider formatProvider);
+     public string ToString(IFormatProvider formatProvider);
    }
  }
}
```

##Code Editing and other Workspace API changes

- The SyntaxGenerator gets support to properly generate accessors.
- There's a new `ImportAdder` api that can be used to point at a region of code and ask it to add the imports for any types in that region. These imports\usings are added with a `Simplifier.Annotation`. The Simplifier has been updated to remove an using with the annotation if it is unused. This combination will be useful to generate some code where the types are simple names and the usings for them are added to the document.
- Some miscellaneous cleanup around making types sealed, removing APIs that were unintentionally public and rounding out some missing APIs in the MSBuildWorkspace.

```diff
 assembly Microsoft.CodeAnalysis.Workspaces {
  namespace Microsoft.CodeAnalysis.Differencing {
    public sealed class Match<TNode> {
      public IEnumerable<KeyValuePair<TNode, TNode>>IReadOnlyDictionary<TNode, TNode> Matches { get; }
+     public IReadOnlyDictionary<TNode, TNode> ReverseMatches { get; }
    }
  }
  namespace Microsoft.CodeAnalysis.Editing {
    public enum DeclarationKind {
+     AddAccessor = 26,
+     GetAccessor = 24,
+     RaiseAccessor = 28,
+     RemoveAccessor = 27,
+     SetAccessor = 25,
    }
    public struct DeclarationModifiers : IEquatable<DeclarationModifiers> {
+     public static readonly DeclarationModifiers WriteOnly;
+     public bool IsWriteOnly { get; }
+     public DeclarationModifiers WithIsWriteOnly(bool isWriteOnly);
    }
+   public static class ImportAdder {
+     public static Task<Document> AddImportsAsync(Document document, OptionSet options=null, CancellationToken cancellationToken=null);
+     public static Task<Document> AddImportsAsync(Document document, SyntaxAnnotation annotation, OptionSet options=null, CancellationToken cancellationToken=null);
+     public static Task<Document> AddImportsAsync(Document document, TextSpan span, OptionSet options=null, CancellationToken cancellationToken=null);
+     public static Task<Document> AddImportsAsync(Document document, IEnumerable<TextSpan> spans, OptionSet options=null, CancellationToken cancellationToken=null);
    }
    public abstract class SyntaxGenerator : ILanguageService {
+     public SyntaxNode AddAccessors(SyntaxNode declaration, IEnumerable<SyntaxNode> accessors);
+     public SyntaxNode GetAccessor(SyntaxNode declaration, DeclarationKind kind);
+     public abstract IReadOnlyList<SyntaxNode> GetAccessors(SyntaxNode declaration);
+     public abstract SyntaxNode InsertAccessors(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> accessors);
    }
  }
  namespace Microsoft.CodeAnalysis.Formatting {
    public static class FormattingOptions {
-     public static readonly PerLanguageOption<bool> UseTabOnlyForIndentation;
    }
  }
  namespace Microsoft.CodeAnalysis.Host {
    public abstract class HostWorkspaceServices {
-     public virtual ITextFactoryService TextFactory { get; }
      public delegate bool MetadataFilter(IReadOnlyDictionary<string, object> metadata);
    }
-   public interface ITextFactoryService : IWorkspaceService {
-     SourceText CreateText(Stream stream, Encoding defaultEncoding, CancellationToken cancellationToken=null);
    }
  }
  namespace Microsoft.CodeAnalysis {
-   public class DocumentId : IEquatable<DocumentId> {
+   public sealed class DocumentId : IEquatable<DocumentId> {
    }
-   public class ProjectId : IEquatable<ProjectId> {
+   public sealed class ProjectId : IEquatable<ProjectId> {
    }
-   public class SolutionId : IEquatable<SolutionId> {
+   public sealed class SolutionId : IEquatable<SolutionId> {
    }
  }
 }
 assembly Microsoft.CodeAnalysis.Workspaces.Desktop {
  namespace Microsoft.CodeAnalysis.MSBuild {
    public sealed class MSBuildWorkspace : Workspace {
-     protected override void ApplyAdditionalDocumentRemoved(DocumentId documentId);
+     protected override void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference);
+     protected override void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference);
+     protected override void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference);
+     protected override void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference);
+     protected override void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference);
+     protected override void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference);
    }
  }
 }
```

##Compiler API changes

- Added an optional parameter to `Compilation.GetSemanticModel` to allow requesting a `SemanticModel` from which ignores accessibility checks for the purpose of lookup. This was done to enable tooling scenarios around debugging and interactive scripting where private/internal members are accessible and should show up in completion lists.
- Added two new base classes to the Syntax API for convenience and to make the API more consistent with the C# Language Specification:
    - Added a new base class `LambdaExpressionSyntax` for `SimpleLambdaExpressionSyntax` (e.g. `x => x`) and `ParenthesizedLambdaExpressionSyntax` (e.g. `(x) => x`).
    - Added a new base class `AnonymousFunctionExpressionSyntax` for `AnonymousMethodExpressionSyntax` (e.g `delegate { return; }`) and `LambdaExpressionSyntax`.
- Added an `CryptoPublicKey` option to `CompilationOptions` to allow specifying a public key for cryptographic signing.
- Added an `Encoding` property to `SyntaxTree` which returns the encoding of the original source text.
- Added a flag to the `SourceText.From` factory method which enables fail-fast when a binary stream is provided rather than text.
- Added compiler support for edit-and-continue scenarios involving lambda and query expressions.
- Fixed a naming inconsistency where properties for semicolons on some nodes (indexers and properties) were incorrectly named `Semicolon` instead of `SemicolonToken` as is the convention elsewhere in the Syntax API.
- Miscellaneous cleanup of non-public members.

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis {
    public abstract class Compilation {
-     protected bool ShouldBeSigned { get; }
-     protected abstract SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree);
+     protected abstract SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility);
-     public SemanticModel GetSemanticModel(SyntaxTree syntaxTree);
+     public SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility=false);
-     protected abstract bool HasCodeToEmit();
    }
    public abstract class CompilationOptions {
+     public ImmutableArray<byte> CryptoPublicKey { get; protected set; }
    }
    
    public abstract class SemanticModel {
+     public virtual bool IgnoresAccessibility { get; }
    }
    public abstract class SyntaxTree {
+     public abstract Encoding Encoding { get; }
    }
-   public class TriggerDiagnosticDescriptor : DiagnosticDescriptor {
-     public TriggerDiagnosticDescriptor(string id, params string[] customTags);
    }
  }
  namespace Microsoft.CodeAnalysis.Emit {
    public struct EditAndContinueMethodDebugInformation {
-     public static EditAndContinueMethodDebugInformation Create(ImmutableArray<byte> compressedSlotMap);
+     public static EditAndContinueMethodDebugInformation Create(ImmutableArray<byte> compressedSlotMap, ImmutableArray<byte> compressedLambdaMap);
    }
  }
  namespace Microsoft.CodeAnalysis.Text {
    public abstract class SourceText {
-     public virtual TextLineCollection Lines { get; }
+     public TextLineCollection Lines { get; }
+     public static SourceText From(byte[] buffer, int length, Encoding encoding=null, SourceHashAlgorithm checksumAlgorithm=(SourceHashAlgorithm)(1), bool throwIfBinaryDetected=false);
-     public static SourceText From(Stream stream, Encoding encoding=null, SourceHashAlgorithm checksumAlgorithm=(SourceHashAlgorithm)(1));
+     public static SourceText From(Stream stream, Encoding encoding=null, SourceHashAlgorithm checksumAlgorithm=(SourceHashAlgorithm)(1), bool throwIfBinaryDetected=false);
+     protected virtual TextLineCollection GetLinesCore();
    }
  }
 }
 assembly Microsoft.CodeAnalysis.CSharp {
  namespace Microsoft.CodeAnalysis.CSharp {
    public sealed class CSharpCompilation : Compilation {
-     protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree);
+     protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility);
-     public new SemanticModel GetSemanticModel(SyntaxTree syntaxTree);
+     public new SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility);
-     protected override bool HasCodeToEmit();
    }
    public sealed class CSharpCompilationOptions : CompilationOptions, IEquatable<CSharpCompilationOptions> {
+     public CSharpCompilationOptions(OutputKind outputKind, string moduleName=null, string mainTypeName=null, string scriptClassName=null, IEnumerable<string> usings=null, OptimizationLevel optimizationLevel=(OptimizationLevel)(0), bool checkOverflow=false, bool allowUnsafe=false, string cryptoKeyContainer=null, string cryptoKeyFile=null, ImmutableArray<byte> cryptoPublicKey=null, Nullable<bool> delaySign=null, Platform platform=(Platform)(0), ReportDiagnostic generalDiagnosticOption=(ReportDiagnostic)(0), int warningLevel=4, IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions=null, bool concurrentBuild=true, XmlReferenceResolver xmlReferenceResolver=null, SourceReferenceResolver sourceReferenceResolver=null, MetadataReferenceResolver metadataReferenceResolver=null, AssemblyIdentityComparer assemblyIdentityComparer=null, StrongNameProvider strongNameProvider=null);
-     public CSharpCompilationOptions(OutputKind outputKind, string moduleName=null, string mainTypeName=null, string scriptClassName=null, IEnumerable<string> usings=null, OptimizationLevel optimizationLevel=(OptimizationLevel)(0), bool checkOverflow=false, bool allowUnsafe=false, string cryptoKeyContainer=null, string cryptoKeyFile=null, Nullable<bool> delaySign=null, Platform platform=(Platform)(0), ReportDiagnostic generalDiagnosticOption=(ReportDiagnostic)(0), int warningLevel=4, IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions=null, bool concurrentBuild=true, XmlReferenceResolver xmlReferenceResolver=null, SourceReferenceResolver sourceReferenceResolver=null, MetadataReferenceResolver metadataReferenceResolver=null, AssemblyIdentityComparer assemblyIdentityComparer=null, StrongNameProvider strongNameProvider=null);
+     public CSharpCompilationOptions WithCryptoPublicKey(ImmutableArray<byte> value);
    }
    public static class SyntaxFactory {
+     public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(CSharpSyntaxNode body);
+     public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(ParameterListSyntax parameterList, CSharpSyntaxNode body);
-     public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(ParameterListSyntax parameterList, BlockSyntax block);
+     public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, CSharpSyntaxNode body);
-     public static AnonymousMethodExpressionSyntax AnonymousMethodExpression(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, BlockSyntax block);
-     public static IndexerDeclarationSyntax IndexerDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken thisKeyword, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolon);
+     public static IndexerDeclarationSyntax IndexerDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken thisKeyword, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken);
-     public static PropertyDeclarationSyntax PropertyDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, EqualsValueClauseSyntax initializer, SyntaxToken semicolon);
+     public static PropertyDeclarationSyntax PropertyDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, EqualsValueClauseSyntax initializer, SyntaxToken semicolonToken);
    }
  }
  namespace Microsoft.CodeAnalysis.CSharp.Syntax {
+   public abstract class AnonymousFunctionExpressionSyntax : ExpressionSyntax {
+     public abstract SyntaxToken AsyncKeyword { get; }
+     public abstract CSharpSyntaxNode Body { get; }
    }
-   public sealed class AnonymousMethodExpressionSyntax : ExpressionSyntax {    
+   public sealed class AnonymousMethodExpressionSyntax : AnonymousFunctionExpressionSyntax {
      public override SyntaxToken AsyncKeyword { get; }
+     public override CSharpSyntaxNode Body { get; }
+     public AnonymousMethodExpressionSyntax Update(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, CSharpSyntaxNode body);
-     public AnonymousMethodExpressionSyntax Update(SyntaxToken asyncKeyword, SyntaxToken delegateKeyword, ParameterListSyntax parameterList, BlockSyntax block);
+     public AnonymousMethodExpressionSyntax WithBody(CSharpSyntaxNode body);
    }
    public sealed class IndexerDeclarationSyntax : BasePropertyDeclarationSyntax {
+     public SyntaxToken SemicolonToken { get; }
      public IndexerDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken thisKeyword, BracketedParameterListSyntax parameterList, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonsemicolonToken);
+     public IndexerDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);
    }
+   public abstract class LambdaExpressionSyntax : AnonymousFunctionExpressionSyntax {
+     public abstract SyntaxToken ArrowToken { get; }
    }
-   public sealed class ParenthesizedLambdaExpressionSyntax : ExpressionSyntax {
+   public sealed class ParenthesizedLambdaExpressionSyntax : LambdaExpressionSyntax {
      public override SyntaxToken ArrowToken { get; }
      public override SyntaxToken AsyncKeyword { get; }
      public override CSharpSyntaxNode Body { get; }
    }
    public sealed class PropertyDeclarationSyntax : BasePropertyDeclarationSyntax {
+     public SyntaxToken SemicolonToken { get; }
      public PropertyDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, SyntaxToken identifier, AccessorListSyntax accessorList, ArrowExpressionClauseSyntax expressionBody, EqualsValueClauseSyntax initializer, SyntaxToken semicolonsemicolonToken);
+     public PropertyDeclarationSyntax WithSemicolonToken(SyntaxToken semicolonToken);
    }
-   public sealed class SimpleLambdaExpressionSyntax : ExpressionSyntax {
+   public sealed class SimpleLambdaExpressionSyntax : LambdaExpressionSyntax {
      public override SyntaxToken ArrowToken { get; }
      public override SyntaxToken AsyncKeyword { get; }
      public override CSharpSyntaxNode Body { get; }
    }
  }
 }
 assembly Microsoft.CodeAnalysis.CSharp.Workspaces {
  namespace Microsoft.CodeAnalysis.CSharp.Formatting {
    public static class CSharpFormattingOptions {
+     public static readonly Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers;
-     public static readonly Option<bool> NewLinesForBracesInObjectInitializers;
    }
  }
 }
 assembly Microsoft.CodeAnalysis.Desktop {
  namespace Microsoft.CodeAnalysis {
    public abstract class SerializableCompilationOptions : ISerializable {
-     protected const string CheckOverflowString = "CheckOverflow";
-     protected const string ConcurrentBuildString = "ConcurrentBuild";
-     protected const string CryptoKeyContainerString = "CryptoKeyContainer";
-     protected const string CryptoKeyFileString = "CryptoKeyFile";
-     protected const string DebugInformationKindString = "DebugInformationKind";
-     protected const string DelaySignString = "DelaySign";
-     protected const string FeaturesString = "Features";
-     protected const string GeneralDiagnosticOptionString = "GeneralDiagnosticOption";
-     protected const string MainTypeNameString = "MainTypeName";
-     protected const string MetadataImportOptionsString = "MetadataImportOptions";
-     protected const string ModuleNameString = "ModuleName";
-     protected const string OptimizeString = "Optimize";
-     protected const string OutputKindString = "OutputKind";
-     protected const string PlatformString = "Platform";
-     protected const string ScriptClassNameString = "ScriptClassName";
-     protected const string SpecificDiagnosticOptionsString = "SpecificDiagnosticOptions";
-     protected const string WarningLevelString = "WarningLevel";
    }
  }
 }
 assembly Microsoft.CodeAnalysis.VisualBasic {
  namespace Microsoft.CodeAnalysis.VisualBasic {
    public sealed class VisualBasicCompilation : Compilation {
-     protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree);
+     protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility);
-     public new SemanticModel GetSemanticModel(SyntaxTree syntaxTree);
+     public new SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility=false);
-     protected override bool HasCodeToEmit();
    }
    public sealed class VisualBasicCompilationOptions : CompilationOptions, IEquatable<VisualBasicCompilationOptions> {
+     public VisualBasicCompilationOptions(OutputKind outputKind, string moduleName=null, string mainTypeName=null, string scriptClassName="Script", IEnumerable<GlobalImport> globalImports=null, string rootNamespace=null, OptionStrict optionStrict=(OptionStrict)(0), bool optionInfer=true, bool optionExplicit=true, bool optionCompareText=false, VisualBasicParseOptions parseOptions=null, bool embedVbCoreRuntime=false, OptimizationLevel optimizationLevel=(OptimizationLevel)(0), bool checkOverflow=true, string cryptoKeyContainer=null, string cryptoKeyFile=null, ImmutableArray<byte> cryptoPublicKey=null, Nullable<bool> delaySign=null, Platform platform=(Platform)(0), ReportDiagnostic generalDiagnosticOption=(ReportDiagnostic)(0), IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions=null, bool concurrentBuild=true, XmlReferenceResolver xmlReferenceResolver=null, SourceReferenceResolver sourceReferenceResolver=null, MetadataReferenceResolver metadataReferenceResolver=null, AssemblyIdentityComparer assemblyIdentityComparer=null, StrongNameProvider strongNameProvider=null);
-     public VisualBasicCompilationOptions(OutputKind outputKind, string moduleName=null, string mainTypeName=null, string scriptClassName="Script", IEnumerable<GlobalImport> globalImports=null, string rootNamespace=null, OptionStrict optionStrict=(OptionStrict)(0), bool optionInfer=true, bool optionExplicit=true, bool optionCompareText=false, VisualBasicParseOptions parseOptions=null, bool embedVbCoreRuntime=false, OptimizationLevel optimizationLevel=(OptimizationLevel)(0), bool checkOverflow=true, string cryptoKeyContainer=null, string cryptoKeyFile=null, Nullable<bool> delaySign=null, Platform platform=(Platform)(0), ReportDiagnostic generalDiagnosticOption=(ReportDiagnostic)(0), IEnumerable<KeyValuePair<string, ReportDiagnostic>> specificDiagnosticOptions=null, bool concurrentBuild=true, XmlReferenceResolver xmlReferenceResolver=null, SourceReferenceResolver sourceReferenceResolver=null, MetadataReferenceResolver metadataReferenceResolver=null, AssemblyIdentityComparer assemblyIdentityComparer=null, StrongNameProvider strongNameProvider=null);
+     public VisualBasicCompilationOptions WithCryptoPublicKey(ImmutableArray<byte> value);
    }
  }
 }

```
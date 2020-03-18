#API changes between VS 2015 Preview and VS 2015 CTP5

##Diagnostics and CodeFix API Changes

We have made some changes to the APIs to better support localization of the various strings that can be returned by analyzers. There are some useful overloads for some common cases that have been added. The `Microsoft.CodeAnalysis.Diagnostics.Internal` namespace and all types there have been made internal as those APIs were just an implementation detail. 

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis {
    public abstract class Diagnostic : IEquatable<Diagnostic>, IFormattable {
-     public abstract string Category { get; }
-     public abstract IReadOnlyList<string> CustomTags { get; }
-     public abstract DiagnosticSeverity DefaultSeverity { get; }
+     public virtual DiagnosticSeverity DefaultSeverity { get; }
-     public abstract string Description { get; }
+     public abstract DiagnosticDescriptor Descriptor { get; }
-     public abstract string HelpLink { get; }
-     public abstract bool IsEnabledByDefault { get; }
+     public static Diagnostic Create(string id, string category, LocalizableString message, DiagnosticSeverity severity, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, int warningLevel, LocalizableString title=null, LocalizableString description=null, string helpLink=null, Location location=null, IEnumerable<Location> additionalLocations=null, IEnumerable<string> customTags=null);
-     public static Diagnostic Create(string id, string category, string message, DiagnosticSeverity severity, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, int warningLevel, string description=null, string helpLink=null, Location location=null, IEnumerable<Location> additionalLocations=null, IEnumerable<string> customTags=null);
-     public abstract string GetMessage(CultureInfo culture=null);
+     public abstract string GetMessage(IFormatProvider formatProvider=null);
+     string System.IFormattable.ToString(string ignored, IFormatProvider formatProvider);
    }
    public class DiagnosticDescriptor {
+     public DiagnosticDescriptor(string id, LocalizableString title, LocalizableString messageFormat, string category, DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, LocalizableString description=null, string helpLink=null, params string[] customTags);
-      public string Description { get; }
+      public LocalizableString Description { get; }
-      public string MessageFormat { get; }
+      public LocalizableString MessageFormat { get; }
-      public string Title { get; }
+      public LocalizableString Title { get; }
+     public override bool Equals(object obj);
+     public override int GetHashCode();
    }
+   public sealed class LocalizableResourceString : LocalizableString {
+     public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource, params string[] formatArguments);
+     public override string ToString(IFormatProvider formatProvider);
    }
+   public abstract class LocalizableString : IFormattable {
+     protected LocalizableString();
+     public static explicit operator string (LocalizableString localizableResource);
+     public static implicit operator LocalizableString (string fixedResource);
+     string System.IFormattable.ToString(string ignored, IFormatProvider formatProvider);
+     public sealed override string ToString();
+     public abstract string ToString(IFormatProvider formatProvider);
    }
  }

  namespace Microsoft.CodeAnalysis.Diagnostics {
    public struct AnalysisContext {
-     public AnalysisContext(SessionStartAnalysisScope scope);
      public void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct, ValueType;
      public void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct, ValueType;
+     public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds);
+     public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct, ValueType;
      public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct, ValueType;
    }
    public class AnalyzerOptions {
-     public AnalyzerOptions(IEnumerable<AdditionalStream> additionalStreams, IDictionary<string, string> globalOptions, CultureInfo culture=null);
+     public AnalyzerOptions(ImmutableArray<AdditionalStream> additionalStreams, ImmutableDictionary<string, string> globalOptions, CultureInfo culture=null);
+     public AnalyzerOptions WithAdditionalStreams(ImmutableArray<AdditionalStream> additionalStreams);
+     public AnalyzerOptions WithCulture(CultureInfo culture);
+     public AnalyzerOptions WithGlobalOptions(ImmutableDictionary<string, string> globalOptions);
    }
    public struct CodeBlockStartAnalysisContext<TLanguageKindEnum> where TLanguageKindEnum : struct, ValueType {
+     public void RegisterSyntaxNodeAction(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds);
    }
    public struct CompilationEndAnalysisContext {
-     public CompilationEndAnalysisContext(Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct CompilationStartAnalysisContext {
-     public CompilationStartAnalysisContext(CompilationStartAnalysisScope scope, Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken);
      public void RegisterCodeBlockEndAction<TLanguageKindEnum>(Action<CodeBlockEndAnalysisContext> action) where TLanguageKindEnum : struct, ValueType;
      public void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) where TLanguageKindEnum : struct, ValueType;
+     public void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds);
+     public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) where TLanguageKindEnum : struct, ValueType;
      public void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct, ValueType;
    }
    public struct SemanticModelAnalysisContext {
-     public SemanticModelAnalysisContext(SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SymbolAnalysisContext {
-     public SymbolAnalysisContext(ISymbol symbol, Compilation compilation, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SyntaxNodeAnalysisContext {
-     public SyntaxNodeAnalysisContext(SyntaxNode node, SemanticModel semanticModel, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
    public struct SyntaxTreeAnalysisContext {
-     public SyntaxTreeAnalysisContext(SyntaxTree tree, AnalyzerOptions options, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken);
    }
  }
}

 assembly Microsoft.CodeAnalysis.Desktop {
  namespace Microsoft.CodeAnalysis {
    public class RuleSet {
-     public RuleSet(string filePath, ReportDiagnostic generalOption, IDictionary<string, ReportDiagnostic> specificOptions, IEnumerable<RuleSetInclude> includes);
+     public RuleSet(string filePath, ReportDiagnostic generalOption, ImmutableDictionary<string, ReportDiagnostic> specificOptions, ImmutableArray<RuleSetInclude> includes);
    }
  }
 }

 assembly Microsoft.CodeAnalysis.Workspaces {
  namespace Microsoft.CodeAnalysis {
    public class Project {
+     public TextDocument AddAdditionalDocument(string name, SourceText text, IEnumerable<string> folders=null);
+     public TextDocument AddAdditionalDocument(string name, string text, IEnumerable<string> folders=null);
+     public Document AddDocument(string name, SyntaxNode syntaxRoot, IEnumerable<string> folders=null);
+     public Project RemoveAdditionalDocument(DocumentId documentId);
    }
    public class Solution {
+     public Solution AddAdditionalDocument(DocumentId documentId, string name, string text, IEnumerable<string> folders=null, string filePath=null);
+     public Solution AddDocument(DocumentId documentId, string name, SyntaxNode syntaxRoot, IEnumerable<string> folders=null, string filePath=null, bool isGenerated=false, PreservationMode preservationMode=(PreservationMode)(0));
    }
    public abstract class Workspace : IDisposable {
      public virtual Solution CurrentSolution { get; }
-     protected virtual void AddAdditionalDocument(DocumentId documentId, IEnumerable<string> folders, string name, SourceText text=null);
+     protected virtual void AddAdditionalDocument(DocumentInfo info, SourceText text);
-     protected virtual void AddDocument(DocumentId documentId, IEnumerable<string> folders, string name, SourceText text=null, SourceCodeKind sourceCodeKind=(SourceCodeKind)(0));
+     protected virtual void AddDocument(DocumentInfo info, SourceText text);
    }
  }

  namespace Microsoft.CodeAnalysis.CodeFixes {
    public struct CodeFixContext {
-     public CodeFixContext(Document document, Diagnostic diagnostic, Action<CodeAction, IEnumerable<Diagnostic>> registerFix, CancellationToken cancellationToken);
+     public CodeFixContext(Document document, Diagnostic diagnostic, Action<CodeAction, ImmutableArray<Diagnostic>> registerFix, CancellationToken cancellationToken);
-     public CodeFixContext(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, Action<CodeAction, IEnumerable<Diagnostic>> registerFix, CancellationToken cancellationToken);
+     public CodeFixContext(Document document, TextSpan span, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction, ImmutableArray<Diagnostic>> registerFix, CancellationToken cancellationToken);
      public IEnumerable<Diagnostic>ImmutableArray<Diagnostic> Diagnostics { get; }
+     public void RegisterFix(CodeAction action, ImmutableArray<Diagnostic> diagnostics);
    }
    public abstract class CodeFixProvider {
-     public abstract FixAllProvider GetFixAllProvider();
+     public virtual FixAllProvider GetFixAllProvider();
    }
  }
}
```

##Changes caused by language features
We've been continuing to work on adding\refining String Interpolation and nameof in both languages and that's changed some APIs.

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis {
    public enum CandidateReason {
+     MemberGroup = 16,
    }
 }

 assembly Microsoft.CodeAnalysis.CSharp {
  namespace Microsoft.CodeAnalysis {
    public static class CSharpExtensions {
+     public static int IndexOf(this SyntaxTokenList list, SyntaxKind kind);
+     public static int IndexOf(this SyntaxTriviaList list, SyntaxKind kind);
+     public static int IndexOf<TNode>(this SeparatedSyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;
+     public static int IndexOf<TNode>(this SyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;
    }
  }
  namespace Microsoft.CodeAnalysis.CSharp {
    public sealed class CSharpCompilation : Compilation {
+     public override bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter=(SymbolFilter)(6), CancellationToken cancellationToken=null);
+     public override IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter=(SymbolFilter)(6), CancellationToken cancellationToken=null);
    }
    public sealed class CSharpParseOptions : ParseOptions, IEquatable<CSharpParseOptions> {
+     public override IReadOnlyDictionary<string, string> Features { get; }
+     protected override ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features);
+     public new CSharpParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features);
    }
    public abstract class CSharpSyntaxRewriter : CSharpSyntaxVisitor<SyntaxNode> {
-     public override SyntaxNode VisitNameOfExpression(NameOfExpressionSyntax node);
    }
    public abstract class CSharpSyntaxVisitor {
-     public virtual void VisitNameOfExpression(NameOfExpressionSyntax node);
    }
    public abstract class CSharpSyntaxVisitor<TResult> {
-     public virtual TResult VisitNameOfExpression(NameOfExpressionSyntax node);
    }
    public enum LanguageVersion {
-     Experimental = 2147483647,
    }
    public static class SyntaxFactory {
+     public static InterpolatedStringInsertSyntax InterpolatedStringInsert(ExpressionSyntax expression, SyntaxToken comma, ExpressionSyntax alignment, SyntaxToken format);
-     public static InterpolatedStringInsertSyntax InterpolatedStringInsert(ExpressionSyntax expression, SyntaxToken comma, ExpressionSyntax alignment, SyntaxToken colon, SyntaxToken format);
      public static SyntaxToken Literal(SyntaxTriviaList leading, string text, decimalDecimal value, SyntaxTriviaList trailing);
      public static SyntaxToken Literal(decimalDecimal value);
      public static SyntaxToken Literal(string text, decimalDecimal value);
-     public static NameOfExpressionSyntax NameOfExpression(IdentifierNameSyntax nameOfIdentifier, ExpressionSyntax argument);
-     public static NameOfExpressionSyntax NameOfExpression(IdentifierNameSyntax nameOfIdentifier, SyntaxToken openParenToken, ExpressionSyntax argument, SyntaxToken closeParenToken);
-     public static NameOfExpressionSyntax NameOfExpression(string nameOfIdentifier, ExpressionSyntax argument);
+     public static UsingDirectiveSyntax UsingDirective(SyntaxToken staticKeyword, NameEqualsSyntax alias, NameSyntax name);
-     public static UsingDirectiveSyntax UsingDirective(SyntaxToken usingKeyword, NameEqualsSyntax alias, NameSyntax name, SyntaxToken semicolonToken);
+     public static UsingDirectiveSyntax UsingDirective(SyntaxToken usingKeyword, SyntaxToken staticKeyword, NameEqualsSyntax alias, NameSyntax name, SyntaxToken semicolonToken);
    }
    public enum SyntaxKind : ushort {
-     NameOfExpression = (ushort)8768,
    }
  }
  namespace Microsoft.CodeAnalysis.CSharp.Syntax {
    public sealed class InterpolatedStringInsertSyntax : CSharpSyntaxNode {
-     public SyntaxToken Colon { get; }
+     public InterpolatedStringInsertSyntax Update(ExpressionSyntax expression, SyntaxToken comma, ExpressionSyntax alignment, SyntaxToken format);
-     public InterpolatedStringInsertSyntax Update(ExpressionSyntax expression, SyntaxToken comma, ExpressionSyntax alignment, SyntaxToken colon, SyntaxToken format);
-     public InterpolatedStringInsertSyntax WithColon(SyntaxToken colon);
    }
-   public sealed class NameOfExpressionSyntax : ExpressionSyntax {
-     public ExpressionSyntax Argument { get; }
-     public SyntaxToken CloseParenToken { get; }
-     public IdentifierNameSyntax NameOfIdentifier { get; }
-     public SyntaxToken OpenParenToken { get; }
-     public override void Accept(CSharpSyntaxVisitor visitor);
-     public override TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);
-     public NameOfExpressionSyntax Update(IdentifierNameSyntax nameOfIdentifier, SyntaxToken openParenToken, ExpressionSyntax argument, SyntaxToken closeParenToken);
-     public NameOfExpressionSyntax WithArgument(ExpressionSyntax argument);
-     public NameOfExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);
-     public NameOfExpressionSyntax WithNameOfIdentifier(IdentifierNameSyntax nameOfIdentifier);
-     public NameOfExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);
    }
    public sealed class UsingDirectiveSyntax : CSharpSyntaxNode {
+     public SyntaxToken StaticKeyword { get; }
-     public UsingDirectiveSyntax Update(SyntaxToken usingKeyword, NameEqualsSyntax alias, NameSyntax name, SyntaxToken semicolonToken);
+     public UsingDirectiveSyntax Update(SyntaxToken usingKeyword, SyntaxToken staticKeyword, NameEqualsSyntax alias, NameSyntax name, SyntaxToken semicolonToken);
+     public UsingDirectiveSyntax WithStaticKeyword(SyntaxToken staticKeyword);
    }
  }
 }

 assembly Microsoft.CodeAnalysis.VisualBasic {
  namespace Microsoft.CodeAnalysis {
    public sealed class VisualBasicExtensions {
+     public static bool Any<TNode>(this SeparatedSyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;
+     public static bool Any<TNode>(this SyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;
+     public static int IndexOf(this SyntaxTokenList list, SyntaxKind kind);
+     public static int IndexOf(this SyntaxTriviaList list, SyntaxKind kind);
+     public static int IndexOf<TNode>(this SeparatedSyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;
+     public static int IndexOf<TNode>(this SyntaxList<TNode> list, SyntaxKind kind) where TNode : SyntaxNode;
    }
  }

  namespace Microsoft.CodeAnalysis.VisualBasic {
    public enum LanguageVersion {
-     Experimental = 2147483647,
    }
    public class SyntaxFactory {
      public static SyntaxToken DecimalLiteralToken(SyntaxTriviaList leadingTrivia, string text, TypeCharacter typeSuffix, decimalDecimal value, SyntaxTriviaList trailingTrivia);
      public static SyntaxToken DecimalLiteralToken(string text, TypeCharacter typeSuffix, decimalDecimal value);
      public static SyntaxToken Literal(SyntaxTriviaList leading, string text, decimalDecimal value, SyntaxTriviaList trailing);
      public static SyntaxToken Literal(decimalDecimal value);
      public static SyntaxToken Literal(string text, decimalDecimal value);
+     public static NameOfExpressionSyntax NameOfExpression(SyntaxToken nameOfKeyword, SyntaxToken openParenToken, ExpressionSyntax argument, SyntaxToken closeParenToken);
+     public static NameOfExpressionSyntax NameOfExpression(ExpressionSyntax argument);
    }
    public enum SyntaxKind : ushort {
+     NameOfExpression = (ushort)779,
+     NameOfKeyword = (ushort)778,
    }
    public sealed class VisualBasicCompilation : Compilation {
+     public override bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter=(SymbolFilter)(6), CancellationToken cancellationToken=null);
+     public override IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter=(SymbolFilter)(6), CancellationToken cancellationToken=null);
    }
    public sealed class VisualBasicParseOptions : ParseOptions, IEquatable<VisualBasicParseOptions> {
+     public override IReadOnlyDictionary<string, string> Features { get; }
+     protected override ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features);
+     public new VisualBasicParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features);
    }
    public abstract class VisualBasicSyntaxRewriter : VisualBasicSyntaxVisitor<SyntaxNode> {
+     public override SyntaxNode VisitNameOfExpression(NameOfExpressionSyntax node);
    }
    public abstract class VisualBasicSyntaxVisitor {
+     public virtual void VisitNameOfExpression(NameOfExpressionSyntax node);
    }
    public abstract class VisualBasicSyntaxVisitor<TResult> {
+     public virtual TResult VisitNameOfExpression(NameOfExpressionSyntax node);
    }
  }
  namespace Microsoft.CodeAnalysis.VisualBasic.Syntax {
+   public sealed class NameOfExpressionSyntax : ExpressionSyntax {
+     public ExpressionSyntax Argument { get; }
+     public SyntaxToken CloseParenToken { get; }
+     public SyntaxToken NameOfKeyword { get; }
+     public SyntaxToken OpenParenToken { get; }
+     public override void Accept(VisualBasicSyntaxVisitor visitor);
+     public override TResult Accept<TResult>(VisualBasicSyntaxVisitor<TResult> visitor);
+     public NameOfExpressionSyntax Update(SyntaxToken nameOfKeyword, SyntaxToken openParenToken, ExpressionSyntax argument, SyntaxToken closeParenToken);
+     public NameOfExpressionSyntax WithArgument(ExpressionSyntax argument);
+     public NameOfExpressionSyntax WithCloseParenToken(SyntaxToken closeParenToken);
+     public NameOfExpressionSyntax WithNameOfKeyword(SyntaxToken nameOfKeyword);
+     public NameOfExpressionSyntax WithOpenParenToken(SyntaxToken openParenToken);
    }
  }
 }
```

##New functionality
We've added some new APIs to make it easier to generate code from Code Fixes\Refactorings and some useful overloads in the SymbolFinder.

```diff
 assembly Microsoft.CodeAnalysis.Workspaces {
  namespace Microsoft.CodeAnalysis.CodeGeneration {
+   public enum DeclarationKind {
+     Attribute = 22,
+     Class = 2,
+     CompilationUnit = 1,
+     Constructor = 10,
+     ConversionOperator = 9,
+     CustomEvent = 17,
+     Delegate = 6,
+     Destructor = 11,
+     Enum = 5,
+     EnumMember = 15,
+     Event = 16,
+     Field = 12,
+     Indexer = 14,
+     Interface = 4,
+     LambdaExpression = 23,
+     LocalVariable = 21,
+     Method = 7,
+     Namespace = 18,
+     NamespaceImport = 19,
+     None = 0,
+     Operator = 8,
+     Parameter = 20,
+     Property = 13,
+     Struct = 3,
    }
    public struct DeclarationModifiers : IEquatable<DeclarationModifiers> {
+     public bool Equals(DeclarationModifiers modifiers);
+     public override bool Equals(object obj);
+     public override int GetHashCode();
+     public static bool operator ==(DeclarationModifiers left, DeclarationModifiers right);
+     public static bool operator !=(DeclarationModifiers left, DeclarationModifiers right);
+     public override string ToString();
    }
+   public class SymbolEditor {
+     public SymbolEditor(Document document);
+     public SymbolEditor(Solution solution);
+     public Solution CurrentSolution { get; }
+     public Solution OriginalSolution { get; }
+     public Task<ISymbol> EditAllDeclarationsAsync(ISymbol symbol, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, ISymbol member, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, Location location, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken=null);
+     public Task<ISymbol> EditOneDeclarationAsync(ISymbol symbol, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> declarationEditor, CancellationToken cancellationToken=null);
+     public IEnumerable<Document> GetChangedDocuments();
+     public Task<ISymbol> GetCurrentSymbolAsync(ISymbol symbol, CancellationToken cancellationToken=null);
    }
    public abstract class SyntaxGenerator : ILanguageService {
-     protected static DeclarationModifiers constructorModifers;
-     protected static DeclarationModifiers fieldModifiers;
-     protected static DeclarationModifiers indexerModifiers;
-     protected static DeclarationModifiers methodModifiers;
-     protected static DeclarationModifiers propertyModifiers;
-     protected static DeclarationModifiers typeModifiers;
+     public static SyntaxRemoveOptions DefaultRemoveOptions;
      public abstract SyntaxNode AddAttributes(SyntaxNode declaration, IEnumerable<SyntaxNode> attributes);
+     public SyntaxNode AddMembers(SyntaxNode declaration, params SyntaxNode[] members);
+     public SyntaxNode AddMembers(SyntaxNode declaration, IEnumerable<SyntaxNode> members);
+     public SyntaxNode AddNamespaceImports(SyntaxNode declaration, params SyntaxNode[] imports);
+     public SyntaxNode AddNamespaceImports(SyntaxNode declaration, IEnumerable<SyntaxNode> imports);
+     public SyntaxNode AddParameters(SyntaxNode declaration, IEnumerable<SyntaxNode> parameters);
      public SyntaxNode AddReturnAttributes(SyntaxNode methodDeclarationdeclaration, params SyntaxNode[] attributes);
      public abstract SyntaxNode AddReturnAttributes(SyntaxNode methodDeclarationdeclaration, IEnumerable<SyntaxNode> attributes);
-     public SyntaxNode AsExpression(SyntaxNode expression, ITypeSymbol type);
-     public abstract SyntaxNode AsExpression(SyntaxNode expression, SyntaxNode type);
+     protected IEnumerable<TNode> ClearTrivia<TNode>(IEnumerable<TNode> nodes) where TNode : SyntaxNode;
+     protected abstract TNode ClearTrivia<TNode>(TNode node) where TNode : SyntaxNode;
      public abstract SyntaxNode CompilationUnit(IEnumerable<SyntaxNode> declarations=null);
+     public SyntaxNode CustomEventDeclaration(IEventSymbol symbol, IEnumerable<SyntaxNode> addAccessorStatements=null, IEnumerable<SyntaxNode> removeAccessorStatements=null);
+     public abstract SyntaxNode CustomEventDeclaration(string name, SyntaxNode type, Accessibility accessibility=(Accessibility)(0), DeclarationModifiers modifiers=null, IEnumerable<SyntaxNode> parameters=null, IEnumerable<SyntaxNode> addAccessorStatements=null, IEnumerable<SyntaxNode> removeAccessorStatements=null);
+     public abstract SyntaxNode DelegateDeclaration(string name, IEnumerable<SyntaxNode> parameters=null, IEnumerable<string> typeParameters=null, SyntaxNode returnType=null, Accessibility accessibility=(Accessibility)(0), DeclarationModifiers modifiers=null);
+     public abstract SyntaxNode EnumDeclaration(string name, Accessibility accessibility=(Accessibility)(0), DeclarationModifiers modifiers=null, IEnumerable<SyntaxNode> members=null);
-     public abstract SyntaxNode EnumDeclaration(string name, Accessibility accessibility=(Accessibility)(0), IEnumerable<SyntaxNode> members=null);
+     public SyntaxNode EventDeclaration(IEventSymbol symbol);
+     public abstract SyntaxNode EventDeclaration(string name, SyntaxNode type, Accessibility accessibility=(Accessibility)(0), DeclarationModifiers modifiers=null);
+     public SyntaxNode FieldDeclaration(IFieldSymbol field);
      public SyntaxNode FieldDeclaration(IFieldSymbol field, SyntaxNode initializer=null);
+     public abstract Accessibility GetAccessibility(SyntaxNode declaration);
+     public abstract IReadOnlyList<SyntaxNode> GetAttributes(SyntaxNode declaration);
+     public SyntaxNode GetDeclaration(SyntaxNode node);
+     public SyntaxNode GetDeclaration(SyntaxNode node, DeclarationKind kind);
+     public abstract DeclarationKind GetDeclarationKind(SyntaxNode declaration);
+     public abstract SyntaxNode GetExpression(SyntaxNode declaration);
+     public static SyntaxGenerator GetGenerator(Document document);
+     public abstract IReadOnlyList<SyntaxNode> GetGetAccessorStatements(SyntaxNode declaration);
+     public abstract IReadOnlyList<SyntaxNode> GetMembers(SyntaxNode declaration);
+     public abstract DeclarationModifiers GetModifiers(SyntaxNode declaration);
+     public abstract string GetName(SyntaxNode declaration);
+     public abstract IReadOnlyList<SyntaxNode> GetNamespaceImports(SyntaxNode declaration);
+     public abstract IReadOnlyList<SyntaxNode> GetParameters(SyntaxNode declaration);
+     public abstract IReadOnlyList<SyntaxNode> GetReturnAttributes(SyntaxNode declaration);
+     public abstract IReadOnlyList<SyntaxNode> GetSetAccessorStatements(SyntaxNode declaration);
+     public abstract IReadOnlyList<SyntaxNode> GetStatements(SyntaxNode declaration);
+     public abstract SyntaxNode GetType(SyntaxNode declaration);
      public SyntaxNode IndexerDeclaration(IPropertySymbol indexer, IEnumerable<SyntaxNode> getterStatementsgetAccessorStatements=null, IEnumerable<SyntaxNode> setterStatementssetAccessorStatements=null);
      public abstract SyntaxNode IndexerDeclaration(IEnumerable<SyntaxNode> parameters, SyntaxNode type, Accessibility accessibility=(Accessibility)(0), DeclarationModifiers modifiers=null, IEnumerable<SyntaxNode> getterStatementsgetAccessorStatements=null, IEnumerable<SyntaxNode> setterStatementssetAccessorStatements=null);
+     public SyntaxNode InsertAttributes(SyntaxNode declaration, int index, params SyntaxNode[] attributes);
+     public abstract SyntaxNode InsertAttributes(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes);
+     public SyntaxNode InsertMembers(SyntaxNode declaration, int index, params SyntaxNode[] members);
+     public abstract SyntaxNode InsertMembers(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> members);
+     public SyntaxNode InsertNamespaceImports(SyntaxNode declaration, int index, params SyntaxNode[] imports);
+     public abstract SyntaxNode InsertNamespaceImports(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> imports);
+     public abstract SyntaxNode InsertParameters(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> parameters);
+     public SyntaxNode InsertReturnAttributes(SyntaxNode declaration, int index, params SyntaxNode[] attributes);
+     public abstract SyntaxNode InsertReturnAttributes(SyntaxNode declaration, int index, IEnumerable<SyntaxNode> attributes);
-     public SyntaxNode IsExpression(SyntaxNode expression, ITypeSymbol type);
-     public abstract SyntaxNode IsExpression(SyntaxNode expression, SyntaxNode type);
+     public SyntaxNode IsTypeExpression(SyntaxNode expression, ITypeSymbol type);
+     public abstract SyntaxNode IsTypeExpression(SyntaxNode expression, SyntaxNode type);
      public abstract SyntaxNode MemberAccessExpression(SyntaxNode expression, SyntaxNode simpleNamememberName);
      public SyntaxNode MemberAccessExpression(SyntaxNode expression, string identifermemberName);
+     protected static SyntaxNode PreserveTrivia<TNode>(TNode node, Func<TNode, SyntaxNode> nodeChanger) where TNode : SyntaxNode;
      public SyntaxNode PropertyDeclaration(IPropertySymbol property, IEnumerable<SyntaxNode> getterStatementsgetAccessorStatements=null, IEnumerable<SyntaxNode> setterStatementssetAccessorStatements=null);
      public abstract SyntaxNode PropertyDeclaration(string name, SyntaxNode type, Accessibility accessibility=(Accessibility)(0), DeclarationModifiers modifiers=null, IEnumerable<SyntaxNode> getterStatementsgetAccessorStatements=null, IEnumerable<SyntaxNode> setterStatementssetAccessorStatements=null);
+     public abstract SyntaxNode RemoveAllAttributes(SyntaxNode declaration);
+     public abstract SyntaxNode RemoveAttributes(SyntaxNode declaration, IEnumerable<SyntaxNode> attributes);
+     public SyntaxNode RemoveMembers(SyntaxNode declaration, params SyntaxNode[] members);
+     public SyntaxNode RemoveMembers(SyntaxNode declaration, IEnumerable<SyntaxNode> members);
+     public SyntaxNode RemoveNamespaceImports(SyntaxNode declaration, params SyntaxNode[] imports);
+     public SyntaxNode RemoveNamespaceImports(SyntaxNode declaration, IEnumerable<SyntaxNode> imports);
+     public SyntaxNode RemoveParameters(SyntaxNode declaration, IEnumerable<SyntaxNode> parameters);
+     public abstract SyntaxNode RemoveReturnAttributes(SyntaxNode declaration, IEnumerable<SyntaxNode> attributes);
+     protected static SyntaxNode ReplaceWithTrivia(SyntaxNode root, SyntaxNode original, SyntaxNode replacement);
+     protected static SyntaxNode ReplaceWithTrivia(SyntaxNode root, SyntaxToken original, SyntaxToken replacement);
+     protected static SyntaxNode ReplaceWithTrivia<TNode>(SyntaxNode root, TNode original, Func<TNode, SyntaxNode> replacer) where TNode : SyntaxNode;
+     public SyntaxNode TryCastExpression(SyntaxNode expression, ITypeSymbol type);
+     public abstract SyntaxNode TryCastExpression(SyntaxNode expression, SyntaxNode type);
+     public SyntaxNode ValueReturningLambdaExpression(SyntaxNode expression);
+     public SyntaxNode ValueReturningLambdaExpression(IEnumerable<SyntaxNode> statements);
+     public SyntaxNode VoidReturningLambdaExpression(SyntaxNode expression);
+     public SyntaxNode VoidReturningLambdaExpression(IEnumerable<SyntaxNode> statements);
+     public abstract SyntaxNode WithAccessibility(SyntaxNode declaration, Accessibility accessibility);
+     public abstract SyntaxNode WithExpression(SyntaxNode declaration, SyntaxNode expression);
+     public abstract SyntaxNode WithGetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements);
+     public abstract SyntaxNode WithModifiers(SyntaxNode declaration, DeclarationModifiers modifiers);
+     public abstract SyntaxNode WithName(SyntaxNode declaration, string name);
+     public abstract SyntaxNode WithSetAccessorStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements);
+     public abstract SyntaxNode WithStatements(SyntaxNode declaration, IEnumerable<SyntaxNode> statements);
+     public abstract SyntaxNode WithType(SyntaxNode declaration, SyntaxNode type);
      public abstract SyntaxNode WithTypeParameters(SyntaxNode declaration, IEnumerable<string> typeParameterNamestypeParameters);
      public SyntaxNode WithTypeParameters(SyntaxNode declaration, params string[] typeParameterNamestypeParameters);
    }
  }

  namespace Microsoft.CodeAnalysis.FindSymbols {
    public static class SymbolFinder {
+     public static Task<IEnumerable<ISymbol>> FindDeclarationsAsync(Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken=null);
+     public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken=null);
+     public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken=null);
+     public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken=null);
+     public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken=null);
    }
  }
}
```

## Miscellaneous
Changes that hide some APIs that shouldn't have been public or weren't fully thought through and changes that add some more useful overloads.

```diff
 assembly Microsoft.CodeAnalysis {
  namespace Microsoft.CodeAnalysis {
    public abstract class Compilation {
+     public abstract bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter=(SymbolFilter)(6), CancellationToken cancellationToken=null);
+     public EmitDifferenceResult EmitDifference(EmitBaseline baseline, IEnumerable<SemanticEdit> edits, Func<ISymbol, bool> isAddedSymbol, Stream metadataStream, Stream ilStream, Stream pdbStream, ICollection<MethodDefinitionHandle> updatedMethods, CancellationToken cancellationToken=null);
+     public abstract IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter=(SymbolFilter)(6), CancellationToken cancellationToken=null);
    }
    public abstract class ParseOptions {
+     public abstract IReadOnlyDictionary<string, string> Features { get; }
+     protected abstract ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features);
+     public ParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features);
    }
    public enum SymbolDisplayExtensionMethodStyle {
+     Default = 0,
      StaticMethod = 02,
    }
+   public enum SymbolFilter {
+     All = 7,
+     Member = 4,
+     Namespace = 1,
+     None = 0,
+     Type = 2,
+     TypeAndMember = 6,
    }
    public static class SyntaxNodeExtensions {
-     public static TRoot ReplaceNode<TRoot, TNode>(this TRoot root, TNode oldNode, IEnumerable<TNode> newNodes) where TRoot : SyntaxNode where TNode : SyntaxNode;
-     public static TRoot ReplaceNode<TRoot, TNode>(this TRoot root, TNode oldNode, TNode newNode) where TRoot : SyntaxNode where TNode : SyntaxNode;
+     public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, SyntaxNode newNode) where TRoot : SyntaxNode;
+     public static TRoot ReplaceNode<TRoot>(this TRoot root, SyntaxNode oldNode, IEnumerable<SyntaxNode> newNodes) where TRoot : SyntaxNode;
    }
    public enum SyntaxRemoveOptions {
+     AddElasticMarker = 32,
    }
  }

 assembly Microsoft.CodeAnalysis.CSharp.Features {
- namespace Microsoft.CodeAnalysis.CSharp.CodeStyle {
-   public static class CSharpCodeStyleOptions {
-     public static readonly Option<bool> UseVarWhenDeclaringLocals;
    }
  }
 }

 assembly Microsoft.CodeAnalysis.EditorFeatures {
+ namespace Microsoft.CodeAnalysis.Editor.Peek {
+   public interface IPeekableItemFactory {
+     Task<IEnumerable<IPeekableItem>> GetPeekableItemsAsync(ISymbol symbol, Project project, IPeekResultFactory peekResultFactory, CancellationToken cancellationToken);
    }
  }
 }

 assembly Microsoft.CodeAnalysis.Workspaces {
  namespace Microsoft.CodeAnalysis {
    public class CustomWorkspace : Workspace {
+     public override bool CanApplyChange(ApplyChangesKind feature);
    }
    public sealed class DocumentInfo {
+     public DocumentInfo WithDefaultEncoding(Encoding encoding);
+     public DocumentInfo WithId(DocumentId id);
+     public DocumentInfo WithName(string name);
    }

  namespace Microsoft.CodeAnalysis.Differencing {
-   public abstract class LongestCommonSubsequence<TSequence> {
-     protected LongestCommonSubsequence();
-     protected double ComputeDistance(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB);
-     protected IEnumerable<LongestCommonSubsequence<TSequence>.Edit> GetEdits(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB);
-     protected IEnumerable<KeyValuePair<int, int>> GetMatchingPairs(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB);
-     protected abstract bool ItemsEqual(TSequence sequenceA, int indexA, TSequence sequenceB, int indexB);
-     protected struct Edit {
-       public readonly EditKind Kind;
-       public readonly int IndexA;
-       public readonly int IndexB;
      }
    }
-   public sealed class LongestCommonSubstring {
-     public static double ComputeDistance(string s1, string s2);
-     protected override bool ItemsEqual(string sequenceA, int indexA, string sequenceB, int indexB);
    }
    public abstract class TreeComparer<TNode> {
      public abstract double GetDistance(TNode leftoldNode, TNode rightnewNode);
      protected internal abstract bool TreesEqual(TNode leftoldNode, TNode rightnewNode);
    }
  }

  namespace Microsoft.CodeAnalysis.Host {
    public abstract class HostLanguageServices {
+     public TLanguageService GetRequiredService<TLanguageService>() where TLanguageService : ILanguageService;
    }
    public abstract class HostWorkspaceServices {
+     public TWorkspaceService GetRequiredService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService;
    }
  }
  namespace Microsoft.CodeAnalysis.Host.Mef {
    public static class ServiceLayer {
+     public const string Desktop = "Desktop";
    }
  }
 }
 assembly Microsoft.CodeAnalysis.Workspaces.Desktop {
  namespace Microsoft.CodeAnalysis {
    public static class CommandLineProject {
-     public static ProjectInfo CreateProjectInfo(Workspace workspace, string projectName, string language, IEnumerable<string> commandLineArgs, string projectDirectory);
-     public static ProjectInfo CreateProjectInfo(Workspace workspace, string projectName, string language, string commandLine, string baseDirectory);
+     public static ProjectInfo CreateProjectInfo(string projectName, string language, IEnumerable<string> commandLineArgs, string projectDirectory, Workspace workspace=null);
+     public static ProjectInfo CreateProjectInfo(string projectName, string language, string commandLine, string baseDirectory, Workspace workspace=null);
    }
  }
  namespace Microsoft.CodeAnalysis.Host.Mef {
+   public static class DesktopMefHostServices {
+     public static MefHostServices DefaultServices { get; }
    }
  }
  namespace Microsoft.CodeAnalysis.MSBuild {
    public sealed class MSBuildWorkspace : Workspace {
-     protected override void AddAdditionalDocument(DocumentId documentId, IEnumerable<string> folders, string name, SourceText text=null);
-     protected override void AddDocument(DocumentId documentId, IEnumerable<string> folders, string name, SourceText text=null, SourceCodeKind sourceCodeKind=(SourceCodeKind)(0));
+     protected override void AddDocument(DocumentInfo info, SourceText text);
-     protected override void ChangedAdditionalDocumentText(DocumentId documentId, SourceText text);
    }
  }
 }
```

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.Binder;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The compilation object is an immutable representation of a single invocation of the
    /// compiler. Although immutable, a compilation is also on-demand, and will realize and cache
    /// data as necessary. A compilation can produce a new compilation from existing compilation
    /// with the application of small deltas. In many cases, it is more efficient than creating a
    /// new compilation from scratch, as the new compilation can reuse information from the old
    /// compilation.
    /// </summary>
    public sealed partial class CSharpCompilation : Compilation
    {
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //
        // Changes to the public interface of this class should remain synchronized with the VB
        // version. Do not make any changes to the public interface without making the corresponding
        // change to the VB version.
        //
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        internal static readonly ParallelOptions DefaultParallelOptions = new ParallelOptions();

        private readonly CSharpCompilationOptions _options;
        private readonly Lazy<Imports> _globalImports;
        private readonly Lazy<Imports> _previousSubmissionImports;
        private readonly Lazy<AliasSymbol> _globalNamespaceAlias;  // alias symbol used to resolve "global::".
        private readonly Lazy<ImplicitNamedTypeSymbol> _scriptClass;

        // All imports (using directives and extern aliases) in syntax trees in this compilation.
        // NOTE: We need to de-dup since the Imports objects that populate the list may be GC'd
        // and re-created.
        private ConcurrentSet<ImportInfo> _lazyImportInfos;

        // Cache the CLS diagnostics for the whole compilation so they aren't computed repeatedly.
        // NOTE: Presently, we do not cache the per-tree diagnostics.
        private ImmutableArray<Diagnostic> _lazyClsComplianceDiagnostics;

        private Conversions _conversions;
        internal Conversions Conversions
        {
            get
            {
                if (_conversions == null)
                {
                    Interlocked.CompareExchange(ref _conversions, new BuckStopsHereBinder(this).Conversions, null);
                }

                return _conversions;
            }
        }

        /// <summary>
        /// Manages anonymous types declared in this compilation. Unifies types that are structurally equivalent.
        /// </summary>
        private readonly AnonymousTypeManager _anonymousTypeManager;

        private NamespaceSymbol _lazyGlobalNamespace;

        internal readonly BuiltInOperators builtInOperators;

        /// <summary>
        /// The <see cref="SourceAssemblySymbol"/> for this compilation. Do not access directly, use Assembly property
        /// instead. This field is lazily initialized by ReferenceManager, ReferenceManager.CacheLockObject must be locked
        /// while ReferenceManager "calculates" the value and assigns it, several threads must not perform duplicate
        /// "calculation" simultaneously.
        /// </summary>
        private SourceAssemblySymbol _lazyAssemblySymbol;

        /// <summary>
        /// Holds onto data related to reference binding.
        /// The manager is shared among multiple compilations that we expect to have the same result of reference binding.
        /// In most cases this can be determined without performing the binding. If the compilation however contains a circular
        /// metadata reference (a metadata reference that refers back to the compilation) we need to avoid sharing of the binding results.
        /// We do so by creating a new reference manager for such compilation.
        /// </summary>
        private ReferenceManager _referenceManager;

        private readonly SyntaxAndDeclarationManager _syntaxAndDeclarations;

        /// <summary>
        /// Contains the main method of this assembly, if there is one.
        /// </summary>
        private EntryPoint _lazyEntryPoint;

        /// <summary>
        /// Emit nullable attributes for only those members that are visible outside the assembly
        /// (public, protected, and if any [InternalsVisibleTo] attributes, internal members).
        /// If false, attributes are emitted for all members regardless of visibility.
        /// </summary>
        private ThreeState _lazyEmitNullablePublicOnly;

        /// <summary>
        /// The set of trees for which a <see cref="CompilationUnitCompletedEvent"/> has been added to the queue.
        /// </summary>
        private HashSet<SyntaxTree> _lazyCompilationUnitCompletedTrees;

        /// <summary>
        /// Run the nullable walker during the flow analysis passes. True if the project-level nullable
        /// context option is set, or if any file enables nullable or just the nullable warnings.
        /// </summary>
        private ThreeState _lazyShouldRunNullableWalker;

        public override string Language
        {
            get
            {
                return LanguageNames.CSharp;
            }
        }

        public override bool IsCaseSensitive
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The options the compilation was created with.
        /// </summary>
        public new CSharpCompilationOptions Options
        {
            get
            {
                return _options;
            }
        }

        internal AnonymousTypeManager AnonymousTypeManager
        {
            get
            {
                return _anonymousTypeManager;
            }
        }

        internal override CommonAnonymousTypeManager CommonAnonymousTypeManager
        {
            get
            {
                return AnonymousTypeManager;
            }
        }

        /// <summary>
        /// True when the compiler is run in "strict" mode, in which it enforces the language specification
        /// in some cases even at the expense of full compatibility. Such differences typically arise when
        /// earlier versions of the compiler failed to enforce the full language specification.
        /// </summary>
        internal bool FeatureStrictEnabled => Feature("strict") != null;

        /// <summary>
        /// True if we should enable nullable semantic analysis in this compilation.
        /// </summary>
        internal bool NullableSemanticAnalysisEnabled
        {
            get
            {
                var nullableAnalysisFlag = Feature("run-nullable-analysis");
                if (nullableAnalysisFlag == "false")
                {
                    return false;
                }

                return ShouldRunNullableWalker || nullableAnalysisFlag == "true";
            }
        }

        /// <summary>
        /// True when the "peverify-compat" feature flag is set or the language version is below C# 7.2.
        /// With this flag we will avoid certain patterns known not be compatible with PEVerify.
        /// The code may be less efficient and may deviate from spec in corner cases.
        /// The flag is only to be used if PEVerify pass is extremely important.
        /// </summary>
        internal bool IsPeVerifyCompatEnabled => LanguageVersion < LanguageVersion.CSharp7_2 || Feature("peverify-compat") != null;

        internal bool ShouldRunNullableWalker
        {
            get
            {
                if (!_lazyShouldRunNullableWalker.HasValue())
                {
                    if (Options.NullableContextOptions != NullableContextOptions.Disable)
                    {
                        _lazyShouldRunNullableWalker = ThreeState.True;
                        return true;
                    }

                    foreach (var syntaxTree in SyntaxTrees)
                    {
                        if (((CSharpSyntaxTree)syntaxTree).HasNullableEnables())
                        {
                            _lazyShouldRunNullableWalker = ThreeState.True;
                            return true;
                        }
                    }

                    _lazyShouldRunNullableWalker = ThreeState.False;
                }

                return _lazyShouldRunNullableWalker.Value();
            }
        }

        /// <summary>
        /// The language version that was used to parse the syntax trees of this compilation.
        /// </summary>
        public LanguageVersion LanguageVersion
        {
            get;
        }

        protected override INamedTypeSymbol CommonCreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity)
        {
            return new ExtendedErrorTypeSymbol(
                       container.EnsureCSharpSymbolOrNull<INamespaceOrTypeSymbol, NamespaceOrTypeSymbol>(nameof(container)),
                       name, arity, errorInfo: null);
        }

        protected override INamespaceSymbol CommonCreateErrorNamespaceSymbol(INamespaceSymbol container, string name)
        {
            return new MissingNamespaceSymbol(
                       container.EnsureCSharpSymbolOrNull<INamespaceSymbol, NamespaceSymbol>(nameof(container)),
                       name);
        }

        #region Constructors and Factories

        private static readonly CSharpCompilationOptions s_defaultOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
        private static readonly CSharpCompilationOptions s_defaultSubmissionOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithReferencesSupersedeLowerVersions(true);

        /// <summary>
        /// Creates a new compilation from scratch. Methods such as AddSyntaxTrees or AddReferences
        /// on the returned object will allow to continue building up the Compilation incrementally.
        /// </summary>
        /// <param name="assemblyName">Simple assembly name.</param>
        /// <param name="syntaxTrees">The syntax trees with the source code for the new compilation.</param>
        /// <param name="references">The references for the new compilation.</param>
        /// <param name="options">The compiler options to use.</param>
        /// <returns>A new compilation.</returns>
        public static CSharpCompilation Create(
            string assemblyName,
            IEnumerable<SyntaxTree> syntaxTrees = null,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null)
        {
            return Create(
                assemblyName,
                options ?? s_defaultOptions,
                syntaxTrees,
                references,
                previousSubmission: null,
                returnType: null,
                hostObjectType: null,
                isSubmission: false);
        }

        /// <summary>
        /// Creates a new compilation that can be used in scripting.
        /// </summary>
        public static CSharpCompilation CreateScriptCompilation(
            string assemblyName,
            SyntaxTree syntaxTree = null,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpCompilation previousScriptCompilation = null,
            Type returnType = null,
            Type globalsType = null)
        {
            CheckSubmissionOptions(options);
            ValidateScriptCompilationParameters(previousScriptCompilation, returnType, ref globalsType);

            return Create(
                assemblyName,
                options?.WithReferencesSupersedeLowerVersions(true) ?? s_defaultSubmissionOptions,
                (syntaxTree != null) ? new[] { syntaxTree } : SpecializedCollections.EmptyEnumerable<SyntaxTree>(),
                references,
                previousScriptCompilation,
                returnType,
                globalsType,
                isSubmission: true);
        }

        private static CSharpCompilation Create(
            string assemblyName,
            CSharpCompilationOptions options,
            IEnumerable<SyntaxTree> syntaxTrees,
            IEnumerable<MetadataReference> references,
            CSharpCompilation previousSubmission,
            Type returnType,
            Type hostObjectType,
            bool isSubmission)
        {
            Debug.Assert(options != null);
            Debug.Assert(!isSubmission || options.ReferencesSupersedeLowerVersions);

            var validatedReferences = ValidateReferences<CSharpCompilationReference>(references);

            // We can't reuse the whole Reference Manager entirely (reuseReferenceManager = false)
            // because the set of references of this submission differs from the previous one.
            // The submission inherits references of the previous submission, adds the previous submission reference
            // and may add more references passed explicitly or via #r.
            //
            // TODO: Consider reusing some results of the assembly binding to improve perf
            // since most of the binding work is similar.

            var compilation = new CSharpCompilation(
                assemblyName,
                options,
                validatedReferences,
                previousSubmission,
                returnType,
                hostObjectType,
                isSubmission,
                referenceManager: null,
                reuseReferenceManager: false,
                syntaxAndDeclarations: new SyntaxAndDeclarationManager(
                    ImmutableArray<SyntaxTree>.Empty,
                    options.ScriptClassName,
                    options.SourceReferenceResolver,
                    CSharp.MessageProvider.Instance,
                    isSubmission,
                    state: null));

            if (syntaxTrees != null)
            {
                compilation = compilation.AddSyntaxTrees(syntaxTrees);
            }

            Debug.Assert((object)compilation._lazyAssemblySymbol == null);
            return compilation;
        }

        private CSharpCompilation(
            string assemblyName,
            CSharpCompilationOptions options,
            ImmutableArray<MetadataReference> references,
            CSharpCompilation previousSubmission,
            Type submissionReturnType,
            Type hostObjectType,
            bool isSubmission,
            ReferenceManager referenceManager,
            bool reuseReferenceManager,
            SyntaxAndDeclarationManager syntaxAndDeclarations,
            AsyncQueue<CompilationEvent> eventQueue = null)
            : this(assemblyName, options, references, previousSubmission, submissionReturnType, hostObjectType, isSubmission, referenceManager, reuseReferenceManager, syntaxAndDeclarations, SyntaxTreeCommonFeatures(syntaxAndDeclarations.ExternalSyntaxTrees), eventQueue)
        {
        }

        private CSharpCompilation(
            string assemblyName,
            CSharpCompilationOptions options,
            ImmutableArray<MetadataReference> references,
            CSharpCompilation previousSubmission,
            Type submissionReturnType,
            Type hostObjectType,
            bool isSubmission,
            ReferenceManager referenceManager,
            bool reuseReferenceManager,
            SyntaxAndDeclarationManager syntaxAndDeclarations,
            IReadOnlyDictionary<string, string> features,
            AsyncQueue<CompilationEvent> eventQueue = null)
            : base(assemblyName, references, features, isSubmission, eventQueue)
        {
            WellKnownMemberSignatureComparer = new WellKnownMembersSignatureComparer(this);
            _options = options;

            this.builtInOperators = new BuiltInOperators(this);
            _scriptClass = new Lazy<ImplicitNamedTypeSymbol>(BindScriptClass);
            _globalImports = new Lazy<Imports>(BindGlobalImports);
            _previousSubmissionImports = new Lazy<Imports>(ExpandPreviousSubmissionImports);
            _globalNamespaceAlias = new Lazy<AliasSymbol>(CreateGlobalNamespaceAlias);
            _anonymousTypeManager = new AnonymousTypeManager(this);
            this.LanguageVersion = CommonLanguageVersion(syntaxAndDeclarations.ExternalSyntaxTrees);

            if (isSubmission)
            {
                Debug.Assert(previousSubmission == null || previousSubmission.HostObjectType == hostObjectType);
                this.ScriptCompilationInfo = new CSharpScriptCompilationInfo(previousSubmission, submissionReturnType, hostObjectType);
            }
            else
            {
                Debug.Assert(previousSubmission == null && submissionReturnType == null && hostObjectType == null);
            }

            if (reuseReferenceManager)
            {
                referenceManager.AssertCanReuseForCompilation(this);
                _referenceManager = referenceManager;
            }
            else
            {
                _referenceManager = new ReferenceManager(
                    MakeSourceAssemblySimpleName(),
                    this.Options.AssemblyIdentityComparer,
                    observedMetadata: referenceManager?.ObservedMetadata);
            }

            _syntaxAndDeclarations = syntaxAndDeclarations;

            Debug.Assert((object)_lazyAssemblySymbol == null);
            if (EventQueue != null) EventQueue.TryEnqueue(new CompilationStartedEvent(this));
        }

        internal override void ValidateDebugEntryPoint(IMethodSymbol debugEntryPoint, DiagnosticBag diagnostics)
        {
            Debug.Assert(debugEntryPoint != null);

            // Debug entry point has to be a method definition from this compilation.
            var methodSymbol = debugEntryPoint as MethodSymbol;
            if (methodSymbol?.DeclaringCompilation != this || !methodSymbol.IsDefinition)
            {
                diagnostics.Add(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition, Location.None);
            }
        }

        private static LanguageVersion CommonLanguageVersion(ImmutableArray<SyntaxTree> syntaxTrees)
        {
            LanguageVersion? result = null;
            foreach (var tree in syntaxTrees)
            {
                var version = ((CSharpParseOptions)tree.Options).LanguageVersion;
                if (result == null)
                {
                    result = version;
                }
                else if (result != version)
                {
                    throw new ArgumentException(CodeAnalysisResources.InconsistentLanguageVersions, nameof(syntaxTrees));
                }
            }

            return result ?? LanguageVersion.Default.MapSpecifiedToEffectiveVersion();
        }

        /// <summary>
        /// Create a duplicate of this compilation with different symbol instances.
        /// </summary>
        public new CSharpCompilation Clone()
        {
            return new CSharpCompilation(
                this.AssemblyName,
                _options,
                this.ExternalReferences,
                this.PreviousSubmission,
                this.SubmissionReturnType,
                this.HostObjectType,
                this.IsSubmission,
                _referenceManager,
                reuseReferenceManager: true,
                syntaxAndDeclarations: _syntaxAndDeclarations);
        }

        private CSharpCompilation Update(
            ReferenceManager referenceManager,
            bool reuseReferenceManager,
            SyntaxAndDeclarationManager syntaxAndDeclarations)
        {
            return new CSharpCompilation(
                this.AssemblyName,
                _options,
                this.ExternalReferences,
                this.PreviousSubmission,
                this.SubmissionReturnType,
                this.HostObjectType,
                this.IsSubmission,
                referenceManager,
                reuseReferenceManager,
                syntaxAndDeclarations);
        }

        /// <summary>
        /// Creates a new compilation with the specified name.
        /// </summary>
        public new CSharpCompilation WithAssemblyName(string assemblyName)
        {
            // Can't reuse references since the source assembly name changed and the referenced symbols might
            // have internals-visible-to relationship with this compilation or they might had a circular reference
            // to this compilation.

            return new CSharpCompilation(
                assemblyName,
                _options,
                this.ExternalReferences,
                this.PreviousSubmission,
                this.SubmissionReturnType,
                this.HostObjectType,
                this.IsSubmission,
                _referenceManager,
                reuseReferenceManager: assemblyName == this.AssemblyName,
                syntaxAndDeclarations: _syntaxAndDeclarations);
        }

        /// <summary>
        /// Creates a new compilation with the specified references.
        /// </summary>
        /// <remarks>
        /// The new <see cref="CSharpCompilation"/> will query the given <see cref="MetadataReference"/> for the underlying
        /// metadata as soon as the are needed.
        ///
        /// The new compilation uses whatever metadata is currently being provided by the <see cref="MetadataReference"/>.
        /// E.g. if the current compilation references a metadata file that has changed since the creation of the compilation
        /// the new compilation is going to use the updated version, while the current compilation will be using the previous (it doesn't change).
        /// </remarks>
        public new CSharpCompilation WithReferences(IEnumerable<MetadataReference> references)
        {
            // References might have changed, don't reuse reference manager.
            // Don't even reuse observed metadata - let the manager query for the metadata again.

            return new CSharpCompilation(
                this.AssemblyName,
                _options,
                ValidateReferences<CSharpCompilationReference>(references),
                this.PreviousSubmission,
                this.SubmissionReturnType,
                this.HostObjectType,
                this.IsSubmission,
                referenceManager: null,
                reuseReferenceManager: false,
                syntaxAndDeclarations: _syntaxAndDeclarations);
        }

        /// <summary>
        /// Creates a new compilation with the specified references.
        /// </summary>
        public new CSharpCompilation WithReferences(params MetadataReference[] references)
        {
            return this.WithReferences((IEnumerable<MetadataReference>)references);
        }

        /// <summary>
        /// Creates a new compilation with the specified compilation options.
        /// </summary>
        public CSharpCompilation WithOptions(CSharpCompilationOptions options)
        {
            var oldOptions = this.Options;
            bool reuseReferenceManager = oldOptions.CanReuseCompilationReferenceManager(options);
            bool reuseSyntaxAndDeclarationManager = oldOptions is { ScriptClassName: options.ScriptClassName, SourceReferenceResolver: options.SourceReferenceResolver };

            return new CSharpCompilation(
                this.AssemblyName,
                options,
                this.ExternalReferences,
                this.PreviousSubmission,
                this.SubmissionReturnType,
                this.HostObjectType,
                this.IsSubmission,
                _referenceManager,
                reuseReferenceManager,
                reuseSyntaxAndDeclarationManager ?
                    _syntaxAndDeclarations :
                    new SyntaxAndDeclarationManager(
                        _syntaxAndDeclarations.ExternalSyntaxTrees,
                        options.ScriptClassName,
                        options.SourceReferenceResolver,
                        _syntaxAndDeclarations.MessageProvider,
                        _syntaxAndDeclarations.IsSubmission,
                        state: null));
        }

        /// <summary>
        /// Returns a new compilation with the given compilation set as the previous submission.
        /// </summary>
        public CSharpCompilation WithScriptCompilationInfo(CSharpScriptCompilationInfo info)
        {
            if (info == ScriptCompilationInfo)
            {
                return this;
            }

            // Reference binding doesn't depend on previous submission so we can reuse it.

            return new CSharpCompilation(
                this.AssemblyName,
                _options,
                this.ExternalReferences,
                info?.PreviousScriptCompilation,
                info?.ReturnTypeOpt,
                info?.GlobalsType,
                info != null,
                _referenceManager,
                reuseReferenceManager: true,
                syntaxAndDeclarations: _syntaxAndDeclarations);
        }

        /// <summary>
        /// Returns a new compilation with a given event queue.
        /// </summary>
        internal override Compilation WithEventQueue(AsyncQueue<CompilationEvent> eventQueue)
        {
            return new CSharpCompilation(
                this.AssemblyName,
                _options,
                this.ExternalReferences,
                this.PreviousSubmission,
                this.SubmissionReturnType,
                this.HostObjectType,
                this.IsSubmission,
                _referenceManager,
                reuseReferenceManager: true,
                syntaxAndDeclarations: _syntaxAndDeclarations,
                eventQueue: eventQueue);
        }

        #endregion

        #region Submission

        public new CSharpScriptCompilationInfo ScriptCompilationInfo { get; }
        internal override ScriptCompilationInfo CommonScriptCompilationInfo => ScriptCompilationInfo;

        internal CSharpCompilation PreviousSubmission => ScriptCompilationInfo?.PreviousScriptCompilation;

        internal override bool HasSubmissionResult()
        {
            Debug.Assert(IsSubmission);

            // A submission may be empty or comprised of a single script file.
            var tree = _syntaxAndDeclarations.ExternalSyntaxTrees.SingleOrDefault();
            if (tree == null)
            {
                return false;
            }

            var root = tree.GetCompilationUnitRoot();
            if (root.HasErrors)
            {
                return false;
            }

            // Are there any top-level return statements?
            if (root.DescendantNodes(n => n is GlobalStatementSyntax || n is StatementSyntax || n is CompilationUnitSyntax).Any(n => n.IsKind(SyntaxKind.ReturnStatement)))
            {
                return true;
            }

            // Is there a trailing expression?
            var lastGlobalStatement = (GlobalStatementSyntax)root.Members.LastOrDefault(m => m.IsKind(SyntaxKind.GlobalStatement));
            if (lastGlobalStatement != null)
            {
                var statement = lastGlobalStatement.Statement;
                if (statement.IsKind(SyntaxKind.ExpressionStatement))
                {
                    var expressionStatement = (ExpressionStatementSyntax)statement;
                    if (expressionStatement.SemicolonToken.IsMissing)
                    {
                        var model = GetSemanticModel(tree);
                        var expression = expressionStatement.Expression;
                        var info = model.GetTypeInfo(expression);
                        return info.ConvertedType?.SpecialType != SpecialType.System_Void;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Syntax Trees (maintain an ordered list)

        /// <summary>
        /// The syntax trees (parsed from source code) that this compilation was created with.
        /// </summary>
        public new ImmutableArray<SyntaxTree> SyntaxTrees
        {
            get { return _syntaxAndDeclarations.GetLazyState().SyntaxTrees; }
        }

        /// <summary>
        /// Returns true if this compilation contains the specified tree.  False otherwise.
        /// </summary>
        public new bool ContainsSyntaxTree(SyntaxTree syntaxTree)
        {
            return syntaxTree != null && _syntaxAndDeclarations.GetLazyState().RootNamespaces.ContainsKey(syntaxTree);
        }

        /// <summary>
        /// Creates a new compilation with additional syntax trees.
        /// </summary>
        public new CSharpCompilation AddSyntaxTrees(params SyntaxTree[] trees)
        {
            return AddSyntaxTrees((IEnumerable<SyntaxTree>)trees);
        }

        /// <summary>
        /// Creates a new compilation with additional syntax trees.
        /// </summary>
        public new CSharpCompilation AddSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            if (trees == null)
            {
                throw new ArgumentNullException(nameof(trees));
            }

            if (trees.IsEmpty())
            {
                return this;
            }

            // This HashSet is needed so that we don't allow adding the same tree twice
            // with a single call to AddSyntaxTrees.  Rather than using a separate HashSet,
            // ReplaceSyntaxTrees can just check against ExternalSyntaxTrees, because we
            // only allow replacing a single tree at a time.
            var externalSyntaxTrees = PooledHashSet<SyntaxTree>.GetInstance();
            var syntaxAndDeclarations = _syntaxAndDeclarations;
            externalSyntaxTrees.AddAll(syntaxAndDeclarations.ExternalSyntaxTrees);
            bool reuseReferenceManager = true;
            int i = 0;
            foreach (var tree in trees.Cast<CSharpSyntaxTree>())
            {
                if (tree == null)
                {
                    throw new ArgumentNullException($"{nameof(trees)}[{i}]");
                }

                if (!tree.HasCompilationUnitRoot)
                {
                    throw new ArgumentException(CSharpResources.TreeMustHaveARootNodeWith, $"{nameof(trees)}[{i}]");
                }

                if (externalSyntaxTrees.Contains(tree))
                {
                    throw new ArgumentException(CSharpResources.SyntaxTreeAlreadyPresent, $"{nameof(trees)}[{i}]");
                }

                if (this.IsSubmission && tree.Options.Kind == SourceCodeKind.Regular)
                {
                    throw new ArgumentException(CSharpResources.SubmissionCanOnlyInclude, $"{nameof(trees)}[{i}]");
                }

                externalSyntaxTrees.Add(tree);
                reuseReferenceManager &= !tree.HasReferenceOrLoadDirectives;

                i++;
            }
            externalSyntaxTrees.Free();

            if (this.IsSubmission && i > 1)
            {
                throw new ArgumentException(CSharpResources.SubmissionCanHaveAtMostOne, nameof(trees));
            }

            syntaxAndDeclarations = syntaxAndDeclarations.AddSyntaxTrees(trees);

            return Update(_referenceManager, reuseReferenceManager, syntaxAndDeclarations);
        }

        /// <summary>
        /// Creates a new compilation without the specified syntax trees. Preserves metadata info for use with trees
        /// added later.
        /// </summary>
        public new CSharpCompilation RemoveSyntaxTrees(params SyntaxTree[] trees)
        {
            return RemoveSyntaxTrees((IEnumerable<SyntaxTree>)trees);
        }

        /// <summary>
        /// Creates a new compilation without the specified syntax trees. Preserves metadata info for use with trees
        /// added later.
        /// </summary>
        public new CSharpCompilation RemoveSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            if (trees == null)
            {
                throw new ArgumentNullException(nameof(trees));
            }

            if (trees.IsEmpty())
            {
                return this;
            }

            var removeSet = PooledHashSet<SyntaxTree>.GetInstance();
            // This HashSet is needed so that we don't allow adding the same tree twice
            // with a single call to AddSyntaxTrees.  Rather than using a separate HashSet,
            // ReplaceSyntaxTrees can just check against ExternalSyntaxTrees, because we
            // only allow replacing a single tree at a time.
            var externalSyntaxTrees = PooledHashSet<SyntaxTree>.GetInstance();
            var syntaxAndDeclarations = _syntaxAndDeclarations;
            externalSyntaxTrees.AddAll(syntaxAndDeclarations.ExternalSyntaxTrees);
            bool reuseReferenceManager = true;
            int i = 0;
            foreach (var tree in trees.Cast<CSharpSyntaxTree>())
            {
                if (!externalSyntaxTrees.Contains(tree))
                {
                    // Check to make sure this is not a #load'ed tree.
                    var loadedSyntaxTreeMap = syntaxAndDeclarations.GetLazyState().LoadedSyntaxTreeMap;
                    if (SyntaxAndDeclarationManager.IsLoadedSyntaxTree(tree, loadedSyntaxTreeMap))
                    {
                        throw new ArgumentException(CSharpResources.SyntaxTreeFromLoadNoRemoveReplace, $"{nameof(trees)}[{i}]");
                    }

                    throw new ArgumentException(CSharpResources.SyntaxTreeNotFoundToRemove, $"{nameof(trees)}[{i}]");
                }

                removeSet.Add(tree);
                reuseReferenceManager &= !tree.HasReferenceOrLoadDirectives;

                i++;
            }
            externalSyntaxTrees.Free();

            syntaxAndDeclarations = syntaxAndDeclarations.RemoveSyntaxTrees(removeSet);
            removeSet.Free();

            return Update(_referenceManager, reuseReferenceManager, syntaxAndDeclarations);
        }

        /// <summary>
        /// Creates a new compilation without any syntax trees. Preserves metadata info
        /// from this compilation for use with trees added later.
        /// </summary>
        public new CSharpCompilation RemoveAllSyntaxTrees()
        {
            var syntaxAndDeclarations = _syntaxAndDeclarations;
            return Update(
                _referenceManager,
                reuseReferenceManager: !syntaxAndDeclarations.MayHaveReferenceDirectives(),
                syntaxAndDeclarations: syntaxAndDeclarations.WithExternalSyntaxTrees(ImmutableArray<SyntaxTree>.Empty));
        }

        /// <summary>
        /// Creates a new compilation without the old tree but with the new tree.
        /// </summary>
        public new CSharpCompilation ReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
        {
            // this is just to force a cast exception
            oldTree = (CSharpSyntaxTree)oldTree;
            newTree = (CSharpSyntaxTree)newTree;

            if (oldTree == null)
            {
                throw new ArgumentNullException(nameof(oldTree));
            }

            if (newTree == null)
            {
                return this.RemoveSyntaxTrees(oldTree);
            }
            else if (newTree == oldTree)
            {
                return this;
            }

            if (!newTree.HasCompilationUnitRoot)
            {
                throw new ArgumentException(CSharpResources.TreeMustHaveARootNodeWith, nameof(newTree));
            }

            var syntaxAndDeclarations = _syntaxAndDeclarations;
            var externalSyntaxTrees = syntaxAndDeclarations.ExternalSyntaxTrees;
            if (!externalSyntaxTrees.Contains(oldTree))
            {
                // Check to see if this is a #load'ed tree.
                var loadedSyntaxTreeMap = syntaxAndDeclarations.GetLazyState().LoadedSyntaxTreeMap;
                if (SyntaxAndDeclarationManager.IsLoadedSyntaxTree(oldTree, loadedSyntaxTreeMap))
                {
                    throw new ArgumentException(CSharpResources.SyntaxTreeFromLoadNoRemoveReplace, nameof(oldTree));
                }

                throw new ArgumentException(CSharpResources.SyntaxTreeNotFoundToRemove, nameof(oldTree));
            }

            if (externalSyntaxTrees.Contains(newTree))
            {
                throw new ArgumentException(CSharpResources.SyntaxTreeAlreadyPresent, nameof(newTree));
            }

            // TODO(tomat): Consider comparing #r's of the old and the new tree. If they are exactly the same we could still reuse.
            // This could be a perf win when editing a script file in the IDE. The services create a new compilation every keystroke
            // that replaces the tree with a new one.
            var reuseReferenceManager = !oldTree.HasReferenceOrLoadDirectives() && !newTree.HasReferenceOrLoadDirectives();
            syntaxAndDeclarations = syntaxAndDeclarations.ReplaceSyntaxTree(oldTree, newTree);

            return Update(_referenceManager, reuseReferenceManager, syntaxAndDeclarations);
        }

        internal override int GetSyntaxTreeOrdinal(SyntaxTree tree)
        {
            Debug.Assert(this.ContainsSyntaxTree(tree));
            return _syntaxAndDeclarations.GetLazyState().OrdinalMap[tree];
        }

        #endregion

        #region References

        internal override CommonReferenceManager CommonGetBoundReferenceManager()
        {
            return GetBoundReferenceManager();
        }

        internal new ReferenceManager GetBoundReferenceManager()
        {
            if ((object)_lazyAssemblySymbol == null)
            {
                _referenceManager.CreateSourceAssemblyForCompilation(this);
                Debug.Assert((object)_lazyAssemblySymbol != null);
            }

            // referenceManager can only be accessed after we initialized the lazyAssemblySymbol.
            // In fact, initialization of the assembly symbol might change the reference manager.
            return _referenceManager;
        }

        // for testing only:
        internal bool ReferenceManagerEquals(CSharpCompilation other)
        {
            return ReferenceEquals(_referenceManager, other._referenceManager);
        }

        public override ImmutableArray<MetadataReference> DirectiveReferences
        {
            get
            {
                return GetBoundReferenceManager().DirectiveReferences;
            }
        }

        internal override IDictionary<(string path, string content), MetadataReference> ReferenceDirectiveMap
            => GetBoundReferenceManager().ReferenceDirectiveMap;

        // for testing purposes
        internal IEnumerable<string> ExternAliases
        {
            get
            {
                return GetBoundReferenceManager().ExternAliases;
            }
        }

        /// <summary>
        /// Gets the <see cref="AssemblySymbol"/> or <see cref="ModuleSymbol"/> for a metadata reference used to create this compilation.
        /// </summary>
        /// <returns><see cref="AssemblySymbol"/> or <see cref="ModuleSymbol"/> corresponding to the given reference or null if there is none.</returns>
        /// <remarks>
        /// Uses object identity when comparing two references.
        /// </remarks>
        internal new Symbol GetAssemblyOrModuleSymbol(MetadataReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (reference.Properties.Kind == MetadataImageKind.Assembly)
            {
                return GetBoundReferenceManager().GetReferencedAssemblySymbol(reference);
            }
            else
            {
                Debug.Assert(reference.Properties.Kind == MetadataImageKind.Module);
                int index = GetBoundReferenceManager().GetReferencedModuleIndex(reference);
                return index < 0 ? null : this.Assembly.Modules[index];
            }
        }

        public override IEnumerable<AssemblyIdentity> ReferencedAssemblyNames
        {
            get
            {
                return Assembly.Modules.SelectMany(module => module.GetReferencedAssemblies());
            }
        }

        /// <summary>
        /// All reference directives used in this compilation.
        /// </summary>
        internal override IEnumerable<ReferenceDirective> ReferenceDirectives
        {
            get { return this.Declarations.ReferenceDirectives; }
        }

        /// <summary>
        /// Returns a metadata reference that a given #r resolves to.
        /// </summary>
        /// <param name="directive">#r directive.</param>
        /// <returns>Metadata reference the specified directive resolves to, or null if the <paramref name="directive"/> doesn't match any #r directive in the compilation.</returns>
        public MetadataReference GetDirectiveReference(ReferenceDirectiveTriviaSyntax directive)
        {
            MetadataReference reference;
            return ReferenceDirectiveMap.TryGetValue((directive.SyntaxTree.FilePath, directive.File.ValueText), out reference) ? reference : null;
        }

        /// <summary>
        /// Creates a new compilation with additional metadata references.
        /// </summary>
        public new CSharpCompilation AddReferences(params MetadataReference[] references)
        {
            return (CSharpCompilation)base.AddReferences(references);
        }

        /// <summary>
        /// Creates a new compilation with additional metadata references.
        /// </summary>
        public new CSharpCompilation AddReferences(IEnumerable<MetadataReference> references)
        {
            return (CSharpCompilation)base.AddReferences(references);
        }

        /// <summary>
        /// Creates a new compilation without the specified metadata references.
        /// </summary>
        public new CSharpCompilation RemoveReferences(params MetadataReference[] references)
        {
            return (CSharpCompilation)base.RemoveReferences(references);
        }

        /// <summary>
        /// Creates a new compilation without the specified metadata references.
        /// </summary>
        public new CSharpCompilation RemoveReferences(IEnumerable<MetadataReference> references)
        {
            return (CSharpCompilation)base.RemoveReferences(references);
        }

        /// <summary>
        /// Creates a new compilation without any metadata references
        /// </summary>
        public new CSharpCompilation RemoveAllReferences()
        {
            return (CSharpCompilation)base.RemoveAllReferences();
        }

        /// <summary>
        /// Creates a new compilation with an old metadata reference replaced with a new metadata reference.
        /// </summary>
        public new CSharpCompilation ReplaceReference(MetadataReference oldReference, MetadataReference newReference)
        {
            return (CSharpCompilation)base.ReplaceReference(oldReference, newReference);
        }

        public override CompilationReference ToMetadataReference(ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false)
        {
            return new CSharpCompilationReference(this, aliases, embedInteropTypes);
        }

        /// <summary>
        /// Get all modules in this compilation, including the source module, added modules, and all
        /// modules of referenced assemblies that do not come from an assembly with an extern alias.
        /// Metadata imported from aliased assemblies is not visible at the source level except through
        /// the use of an extern alias directive. So exclude them from this list which is used to construct
        /// the global namespace.
        /// </summary>
        private void GetAllUnaliasedModules(ArrayBuilder<ModuleSymbol> modules)
        {
            // NOTE: This includes referenced modules - they count as modules of the compilation assembly.
            modules.AddRange(Assembly.Modules);

            var referenceManager = GetBoundReferenceManager();

            for (int i = 0; i < referenceManager.ReferencedAssemblies.Length; i++)
            {
                if (referenceManager.DeclarationsAccessibleWithoutAlias(i))
                {
                    modules.AddRange(referenceManager.ReferencedAssemblies[i].Modules);
                }
            }
        }

        /// <summary>
        /// Return a list of assembly symbols than can be accessed without using an alias.
        /// For example:
        ///   1) /r:A.dll /r:B.dll -> A, B
        ///   2) /r:Goo=A.dll /r:B.dll -> B
        ///   3) /r:Goo=A.dll /r:A.dll -> A
        /// </summary>
        internal void GetUnaliasedReferencedAssemblies(ArrayBuilder<AssemblySymbol> assemblies)
        {
            var referenceManager = GetBoundReferenceManager();

            for (int i = 0; i < referenceManager.ReferencedAssemblies.Length; i++)
            {
                if (referenceManager.DeclarationsAccessibleWithoutAlias(i))
                {
                    assemblies.Add(referenceManager.ReferencedAssemblies[i]);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="MetadataReference"/> that corresponds to the assembly symbol.
        /// </summary>
        public new MetadataReference GetMetadataReference(IAssemblySymbol assemblySymbol)
        {
            return base.GetMetadataReference(assemblySymbol);
        }

        #endregion

        #region Symbols

        /// <summary>
        /// The AssemblySymbol that represents the assembly being created.
        /// </summary>
        internal SourceAssemblySymbol SourceAssembly
        {
            get
            {
                GetBoundReferenceManager();
                return _lazyAssemblySymbol;
            }
        }

        /// <summary>
        /// The AssemblySymbol that represents the assembly being created.
        /// </summary>
        internal new AssemblySymbol Assembly
        {
            get
            {
                return SourceAssembly;
            }
        }

        /// <summary>
        /// Get a ModuleSymbol that refers to the module being created by compiling all of the code.
        /// By getting the GlobalNamespace property of that module, all of the namespaces and types
        /// defined in source code can be obtained.
        /// </summary>
        internal new ModuleSymbol SourceModule
        {
            get
            {
                return Assembly.Modules[0];
            }
        }

        /// <summary>
        /// Gets the root namespace that contains all namespaces and types defined in source code or in
        /// referenced metadata, merged into a single namespace hierarchy.
        /// </summary>
        internal new NamespaceSymbol GlobalNamespace
        {
            get
            {
                if ((object)_lazyGlobalNamespace == null)
                {
                    // Get the root namespace from each module, and merge them all together
                    // Get all modules in this compilation, ones referenced directly by the compilation
                    // as well as those referenced by all referenced assemblies.

                    var modules = ArrayBuilder<ModuleSymbol>.GetInstance();
                    GetAllUnaliasedModules(modules);

                    var result = MergedNamespaceSymbol.Create(
                        new NamespaceExtent(this),
                        null,
                        modules.SelectDistinct(m => m.GlobalNamespace));

                    modules.Free();

                    Interlocked.CompareExchange(ref _lazyGlobalNamespace, result, null);
                }

                return _lazyGlobalNamespace;
            }
        }

        /// <summary>
        /// Given for the specified module or assembly namespace, gets the corresponding compilation
        /// namespace (merged namespace representation for all namespace declarations and references
        /// with contributions for the namespaceSymbol).  Can return null if no corresponding
        /// namespace can be bound in this compilation with the same name.
        /// </summary>
        internal new NamespaceSymbol GetCompilationNamespace(INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol is NamespaceSymbol &&
                namespaceSymbol.NamespaceKind == NamespaceKind.Compilation &&
                namespaceSymbol.ContainingCompilation == this)
            {
                return (NamespaceSymbol)namespaceSymbol;
            }

            var containingNamespace = namespaceSymbol.ContainingNamespace;
            if (containingNamespace == null)
            {
                return this.GlobalNamespace;
            }

            var current = GetCompilationNamespace(containingNamespace);
            if ((object)current != null)
            {
                return current.GetNestedNamespace(namespaceSymbol.Name);
            }

            return null;
        }

        private ConcurrentDictionary<string, NamespaceSymbol> _externAliasTargets;

        internal bool GetExternAliasTarget(string aliasName, out NamespaceSymbol @namespace)
        {
            if (_externAliasTargets == null)
            {
                Interlocked.CompareExchange(ref _externAliasTargets, new ConcurrentDictionary<string, NamespaceSymbol>(), null);
            }
            else if (_externAliasTargets.TryGetValue(aliasName, out @namespace))
            {
                return !(@namespace is MissingNamespaceSymbol);
            }

            ArrayBuilder<NamespaceSymbol> builder = null;
            var referenceManager = GetBoundReferenceManager();
            for (int i = 0; i < referenceManager.ReferencedAssemblies.Length; i++)
            {
                if (referenceManager.AliasesOfReferencedAssemblies[i].Contains(aliasName))
                {
                    builder = builder ?? ArrayBuilder<NamespaceSymbol>.GetInstance();
                    builder.Add(referenceManager.ReferencedAssemblies[i].GlobalNamespace);
                }
            }

            bool foundNamespace = builder != null;

            // We want to cache failures as well as successes so that subsequent incorrect extern aliases with the
            // same alias will have the same target.
            @namespace = foundNamespace
                ? MergedNamespaceSymbol.Create(new NamespaceExtent(this), namespacesToMerge: builder.ToImmutableAndFree(), containingNamespace: null, nameOpt: null)
                : new MissingNamespaceSymbol(new MissingModuleSymbol(new MissingAssemblySymbol(new AssemblyIdentity(System.Guid.NewGuid().ToString())), ordinal: -1));

            // Use GetOrAdd in case another thread beat us to the punch (i.e. should return the same object for the same alias, every time).
            @namespace = _externAliasTargets.GetOrAdd(aliasName, @namespace);

            Debug.Assert(foundNamespace == !(@namespace is MissingNamespaceSymbol));

            return foundNamespace;
        }

        /// <summary>
        /// A symbol representing the implicit Script class. This is null if the class is not
        /// defined in the compilation.
        /// </summary>
        internal new NamedTypeSymbol ScriptClass
        {
            get { return _scriptClass.Value; }
        }

        /// <summary>
        /// Resolves a symbol that represents script container (Script class). Uses the
        /// full name of the container class stored in <see cref="CompilationOptions.ScriptClassName"/> to find the symbol.
        /// </summary>
        /// <returns>The Script class symbol or null if it is not defined.</returns>
        private ImplicitNamedTypeSymbol BindScriptClass()
        {
            return (ImplicitNamedTypeSymbol)CommonBindScriptClass();
        }

        internal bool IsSubmissionSyntaxTree(SyntaxTree tree)
        {
            Debug.Assert(tree != null);
            Debug.Assert(!this.IsSubmission || _syntaxAndDeclarations.ExternalSyntaxTrees.Length <= 1);
            return this.IsSubmission && tree == _syntaxAndDeclarations.ExternalSyntaxTrees.SingleOrDefault();
        }

        /// <summary>
        /// Global imports (including those from previous submissions, if there are any).
        /// </summary>
        internal Imports GlobalImports => _globalImports.Value;

        private Imports BindGlobalImports() => Imports.FromGlobalUsings(this);

        /// <summary>
        /// Imports declared by this submission (null if this isn't one).
        /// </summary>
        internal Imports GetSubmissionImports()
        {
            Debug.Assert(this.IsSubmission);
            Debug.Assert(_syntaxAndDeclarations.ExternalSyntaxTrees.Length <= 1);

            // A submission may be empty or comprised of a single script file.
            var tree = _syntaxAndDeclarations.ExternalSyntaxTrees.SingleOrDefault();
            if (tree == null)
            {
                return Imports.Empty;
            }

            var binder = GetBinderFactory(tree).GetImportsBinder((CSharpSyntaxNode)tree.GetRoot());
            return binder.GetImports(basesBeingResolved: null);
        }

        /// <summary>
        /// Imports from all previous submissions.
        /// </summary>
        internal Imports GetPreviousSubmissionImports() => _previousSubmissionImports.Value;

        private Imports ExpandPreviousSubmissionImports()
        {
            Debug.Assert(this.IsSubmission);
            var previous = this.PreviousSubmission;

            if (previous == null)
            {
                return Imports.Empty;
            }

            return Imports.ExpandPreviousSubmissionImports(previous.GetPreviousSubmissionImports(), this).Concat(
                Imports.ExpandPreviousSubmissionImports(previous.GetSubmissionImports(), this));
        }

        internal AliasSymbol GlobalNamespaceAlias
        {
            get
            {
                return _globalNamespaceAlias.Value;
            }
        }

        /// <summary>
        /// Get the symbol for the predefined type from the COR Library referenced by this compilation.
        /// </summary>
        internal new NamedTypeSymbol GetSpecialType(SpecialType specialType)
        {
            if (specialType <= SpecialType.None || specialType > SpecialType.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(specialType), $"Unexpected SpecialType: '{(int)specialType}'.");
            }

            NamedTypeSymbol result;
            if (IsTypeMissing(specialType))
            {
                MetadataTypeName emittedName = MetadataTypeName.FromFullName(specialType.GetMetadataName(), useCLSCompliantNameArityEncoding: true);
                result = new MissingMetadataTypeSymbol.TopLevel(Assembly.CorLibrary.Modules[0], ref emittedName, specialType);
            }
            else
            {
                result = Assembly.GetSpecialType(specialType);
            }

            Debug.Assert(result.SpecialType == specialType);
            return result;
        }

        /// <summary>
        /// Get the symbol for the predefined type member from the COR Library referenced by this compilation.
        /// </summary>
        internal Symbol GetSpecialTypeMember(SpecialMember specialMember)
        {
            return Assembly.GetSpecialTypeMember(specialMember);
        }

        internal override ISymbol CommonGetSpecialTypeMember(SpecialMember specialMember)
        {
            return GetSpecialTypeMember(specialMember);
        }

        internal TypeSymbol GetTypeByReflectionType(Type type, DiagnosticBag diagnostics)
        {
            var result = Assembly.GetTypeByReflectionType(type, includeReferences: true);
            if ((object)result == null)
            {
                var errorType = new ExtendedErrorTypeSymbol(this, type.Name, 0, CreateReflectionTypeNotFoundError(type));
                diagnostics.Add(errorType.ErrorInfo, NoLocation.Singleton);
                result = errorType;
            }

            return result;
        }

        private static CSDiagnosticInfo CreateReflectionTypeNotFoundError(Type type)
        {
            // The type or namespace name '{0}' could not be found in the global namespace (are you missing an assembly reference?)
            return new CSDiagnosticInfo(
                ErrorCode.ERR_GlobalSingleTypeNameNotFound,
                new object[] { type.AssemblyQualifiedName },
                ImmutableArray<Symbol>.Empty,
                ImmutableArray<Location>.Empty
            );
        }

        // The type of host object model if available.
        private TypeSymbol _lazyHostObjectTypeSymbol;

        internal TypeSymbol GetHostObjectTypeSymbol()
        {
            if (HostObjectType != null && (object)_lazyHostObjectTypeSymbol == null)
            {
                TypeSymbol symbol = Assembly.GetTypeByReflectionType(HostObjectType, includeReferences: true);

                if ((object)symbol == null)
                {
                    MetadataTypeName mdName = MetadataTypeName.FromNamespaceAndTypeName(HostObjectType.Namespace ?? String.Empty,
                                                                                        HostObjectType.Name,
                                                                                        useCLSCompliantNameArityEncoding: true);

                    symbol = new MissingMetadataTypeSymbol.TopLevelWithCustomErrorInfo(
                        new MissingAssemblySymbol(AssemblyIdentity.FromAssemblyDefinition(HostObjectType.GetTypeInfo().Assembly)).Modules[0],
                        ref mdName,
                        CreateReflectionTypeNotFoundError(HostObjectType),
                        SpecialType.None);
                }

                Interlocked.CompareExchange(ref _lazyHostObjectTypeSymbol, symbol, null);
            }

            return _lazyHostObjectTypeSymbol;
        }

        internal SynthesizedInteractiveInitializerMethod GetSubmissionInitializer()
        {
            return (IsSubmission && (object)ScriptClass != null) ?
                ScriptClass.GetScriptInitializer() :
                null;
        }

        /// <summary>
        /// Gets the type within the compilation's assembly and all referenced assemblies (other than
        /// those that can only be referenced via an extern alias) using its canonical CLR metadata name.
        /// </summary>
        internal new NamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            return this.Assembly.GetTypeByMetadataName(fullyQualifiedMetadataName, includeReferences: true, isWellKnownType: false, conflicts: out var _);
        }

        /// <summary>
        /// The TypeSymbol for the type 'dynamic' in this Compilation.
        /// </summary>
        internal new TypeSymbol DynamicType
        {
            get
            {
                return AssemblySymbol.DynamicType;
            }
        }

        /// <summary>
        /// The NamedTypeSymbol for the .NET System.Object type, which could have a TypeKind of
        /// Error if there was no COR Library in this Compilation.
        /// </summary>
        internal new NamedTypeSymbol ObjectType
        {
            get
            {
                return this.Assembly.ObjectType;
            }
        }

        internal bool DeclaresTheObjectClass
        {
            get
            {
                return SourceAssembly.DeclaresTheObjectClass;
            }
        }

        internal new MethodSymbol GetEntryPoint(CancellationToken cancellationToken)
        {
            EntryPoint entryPoint = GetEntryPointAndDiagnostics(cancellationToken);
            return entryPoint?.MethodSymbol;
        }

        internal EntryPoint GetEntryPointAndDiagnostics(CancellationToken cancellationToken)
        {
            if (!this.Options.OutputKind.IsApplication() && ((object)this.ScriptClass == null))
            {
                return null;
            }

            if (this.Options.MainTypeName != null && !this.Options.MainTypeName.IsValidClrTypeName())
            {
                Debug.Assert(!this.Options.Errors.IsDefaultOrEmpty);
                return new EntryPoint(null, ImmutableArray<Diagnostic>.Empty);
            }

            if (_lazyEntryPoint == null)
            {
                ImmutableArray<Diagnostic> diagnostics;
                var entryPoint = FindEntryPoint(cancellationToken, out diagnostics);
                Interlocked.CompareExchange(ref _lazyEntryPoint, new EntryPoint(entryPoint, diagnostics), null);
            }

            return _lazyEntryPoint;
        }

        private MethodSymbol FindEntryPoint(CancellationToken cancellationToken, out ImmutableArray<Diagnostic> sealedDiagnostics)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var entryPointCandidates = ArrayBuilder<MethodSymbol>.GetInstance();

            try
            {
                NamedTypeSymbol mainType;

                string mainTypeName = this.Options.MainTypeName;
                NamespaceSymbol globalNamespace = this.SourceModule.GlobalNamespace;

                if (mainTypeName != null)
                {
                    // Global code is the entry point, ignore all other Mains.
                    var scriptClass = this.ScriptClass;
                    if ((object)scriptClass != null)
                    {
                        // CONSIDER: we could use the symbol instead of just the name.
                        diagnostics.Add(ErrorCode.WRN_MainIgnored, NoLocation.Singleton, mainTypeName);
                        return scriptClass.GetScriptEntryPoint();
                    }

                    var mainTypeOrNamespace = globalNamespace.GetNamespaceOrTypeByQualifiedName(mainTypeName.Split('.')).OfMinimalArity();
                    if ((object)mainTypeOrNamespace == null)
                    {
                        diagnostics.Add(ErrorCode.ERR_MainClassNotFound, NoLocation.Singleton, mainTypeName);
                        return null;
                    }

                    mainType = mainTypeOrNamespace as NamedTypeSymbol;
                    if ((object)mainType == null || mainType.IsGenericType || (mainType.TypeKind != TypeKind.Class && mainType.TypeKind != TypeKind.Struct && !mainType.IsInterface))
                    {
                        diagnostics.Add(ErrorCode.ERR_MainClassNotClass, mainTypeOrNamespace.Locations.First(), mainTypeOrNamespace);
                        return null;
                    }

                    AddEntryPointCandidates(entryPointCandidates, mainType.GetMembersUnordered());
                }
                else
                {
                    mainType = null;

                    AddEntryPointCandidates(
                        entryPointCandidates,
                        this.GetSymbolsWithName(WellKnownMemberNames.EntryPointMethodName, SymbolFilter.Member, cancellationToken));

                    // Global code is the entry point, ignore all other Mains.
                    var scriptClass = this.ScriptClass;
                    if ((object)scriptClass != null)
                    {
                        foreach (var main in entryPointCandidates)
                        {
                            diagnostics.Add(ErrorCode.WRN_MainIgnored, main.Locations.First(), main);
                        }
                        return scriptClass.GetScriptEntryPoint();
                    }
                }

                // Validity and diagnostics are also tracked because they must be conditionally handled
                // if there are not any "traditional" entrypoints found.
                var taskEntryPoints = ArrayBuilder<(bool IsValid, MethodSymbol Candidate, DiagnosticBag SpecificDiagnostics)>.GetInstance();

                // These diagnostics (warning only) are added to the compilation only if
                // there were not any main methods found.
                DiagnosticBag noMainFoundDiagnostics = DiagnosticBag.GetInstance();

                bool CheckValid(MethodSymbol candidate, bool isCandidate, DiagnosticBag specificDiagnostics)
                {
                    if (!isCandidate)
                    {
                        noMainFoundDiagnostics.Add(ErrorCode.WRN_InvalidMainSig, candidate.Locations.First(), candidate);
                        noMainFoundDiagnostics.AddRange(specificDiagnostics);
                        return false;
                    }

                    if (candidate.IsGenericMethod || candidate.ContainingType.IsGenericType)
                    {
                        // a single error for partial methods:
                        noMainFoundDiagnostics.Add(ErrorCode.WRN_MainCantBeGeneric, candidate.Locations.First(), candidate);
                        return false;
                    }
                    return true;
                }

                var viableEntryPoints = ArrayBuilder<MethodSymbol>.GetInstance();

                foreach (var candidate in entryPointCandidates)
                {
                    var perCandidateBag = DiagnosticBag.GetInstance();
                    var (IsCandidate, IsTaskLike) = HasEntryPointSignature(candidate, perCandidateBag);

                    if (IsTaskLike)
                    {
                        taskEntryPoints.Add((IsCandidate, candidate, perCandidateBag));
                    }
                    else
                    {
                        if (CheckValid(candidate, IsCandidate, perCandidateBag))
                        {
                            if (candidate.IsAsync)
                            {
                                diagnostics.Add(ErrorCode.ERR_NonTaskMainCantBeAsync, candidate.Locations.First(), candidate);
                            }
                            else
                            {
                                diagnostics.AddRange(perCandidateBag);
                                viableEntryPoints.Add(candidate);
                            }
                        }
                        perCandidateBag.Free();
                    }
                }

                if (viableEntryPoints.Count == 0)
                {
                    foreach (var (IsValid, Candidate, SpecificDiagnostics) in taskEntryPoints)
                    {
                        if (CheckValid(Candidate, IsValid, SpecificDiagnostics) &&
                            CheckFeatureAvailability(Candidate.ExtractReturnTypeSyntax(), MessageID.IDS_FeatureAsyncMain, diagnostics))
                        {
                            diagnostics.AddRange(SpecificDiagnostics);
                            viableEntryPoints.Add(Candidate);
                        }
                    }
                }

                foreach (var (_, _, SpecificDiagnostics) in taskEntryPoints)
                {
                    SpecificDiagnostics.Free();
                }

                if (viableEntryPoints.Count == 0)
                {
                    diagnostics.AddRange(noMainFoundDiagnostics);
                }
                else if ((object)mainType == null)
                {
                    // Filters out diagnostics so that only InvalidMainSig and MainCant'BeGeneric are left.
                    // The reason that Error diagnostics can end up in `noMainFoundDiagnostics` is when
                    // HasEntryPointSignature yields some Error Diagnostics when people implement Task or Task<T> incorrectly.
                    //
                    // We can't add those Errors to the general diagnostics bag because it would break previously-working programs.
                    // The fact that these warnings are not added when csc is invoked with /main is possibly a bug, and is tracked at
                    // https://github.com/dotnet/roslyn/issues/18964
                    foreach (var diagnostic in noMainFoundDiagnostics.AsEnumerable())
                    {
                        if (diagnostic.Code == (int)ErrorCode.WRN_InvalidMainSig || diagnostic.Code == (int)ErrorCode.WRN_MainCantBeGeneric)
                        {
                            diagnostics.Add(diagnostic);
                        }
                    }
                }

                MethodSymbol entryPoint = null;
                if (viableEntryPoints.Count == 0)
                {
                    if ((object)mainType == null)
                    {
                        diagnostics.Add(ErrorCode.ERR_NoEntryPoint, NoLocation.Singleton);
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_NoMainInClass, mainType.Locations.First(), mainType);
                    }
                }
                else if (viableEntryPoints.Count > 1)
                {
                    viableEntryPoints.Sort(LexicalOrderSymbolComparer.Instance);
                    var info = new CSDiagnosticInfo(
                         ErrorCode.ERR_MultipleEntryPoints,
                         args: Array.Empty<object>(),
                         symbols: viableEntryPoints.OfType<Symbol>().AsImmutable(),
                         additionalLocations: viableEntryPoints.Select(m => m.Locations.First()).OfType<Location>().AsImmutable());

                    diagnostics.Add(new CSDiagnostic(info, viableEntryPoints.First().Locations.First()));
                }
                else
                {
                    entryPoint = viableEntryPoints[0];
                }

                taskEntryPoints.Free();
                viableEntryPoints.Free();
                noMainFoundDiagnostics.Free();
                return entryPoint;
            }
            finally
            {
                entryPointCandidates.Free();
                sealedDiagnostics = diagnostics.ToReadOnlyAndFree();
            }
        }

        private static void AddEntryPointCandidates(
            ArrayBuilder<MethodSymbol> entryPointCandidates, IEnumerable<ISymbol> members)
        {
            foreach (var member in members)
            {
                if (member is MethodSymbol method &&
                    method.IsEntryPointCandidate)
                {
                    entryPointCandidates.Add(method);
                }
            }
        }

        internal bool ReturnsAwaitableToVoidOrInt(MethodSymbol method, DiagnosticBag diagnostics)
        {
            // Common case optimization
            if (method.ReturnType.IsVoidType() || method.ReturnType.SpecialType == SpecialType.System_Int32)
            {
                return false;
            }

            if (!(method.ReturnType is NamedTypeSymbol namedType))
            {
                return false;
            }

            // Early bail so we only ever check things that are System.Threading.Tasks.Task(<T>)
            if (!(TypeSymbol.Equals(namedType.ConstructedFrom, GetWellKnownType(WellKnownType.System_Threading_Tasks_Task), TypeCompareKind.ConsiderEverything2) ||
                  TypeSymbol.Equals(namedType.ConstructedFrom, GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T), TypeCompareKind.ConsiderEverything2)))
            {
                return false;
            }

            var syntax = method.ExtractReturnTypeSyntax();
            var dumbInstance = new BoundLiteral(syntax, ConstantValue.Null, namedType);
            var binder = GetBinder(syntax);
            BoundExpression result;
            var success = binder.GetAwaitableExpressionInfo(dumbInstance, out result, syntax, diagnostics);

            return success &&
                (result.Type.IsVoidType() || result.Type.SpecialType == SpecialType.System_Int32);
        }

        /// <summary>
        /// Checks if the method has an entry point compatible signature, i.e.
        /// - the return type is either void, int, or returns a <see cref="System.Threading.Tasks.Task" />,
        /// or <see cref="System.Threading.Tasks.Task{T}" /> where the return type of GetAwaiter().GetResult()
        /// is either void or int.
        /// - has either no parameter or a single parameter of type string[]
        /// </summary>
        private (bool IsCandidate, bool IsTaskLike) HasEntryPointSignature(MethodSymbol method, DiagnosticBag bag)
        {
            if (method.IsVararg)
            {
                return (false, false);
            }

            TypeSymbol returnType = method.ReturnType;
            bool returnsTaskOrTaskOfInt = false;
            if (returnType.SpecialType != SpecialType.System_Int32 && !returnType.IsVoidType())
            {
                // Never look for ReturnsAwaitableToVoidOrInt on int32 or void
                returnsTaskOrTaskOfInt = ReturnsAwaitableToVoidOrInt(method, bag);
                if (!returnsTaskOrTaskOfInt)
                {
                    return (false, false);
                }
            }

            if (method.RefKind != RefKind.None)
            {
                return (false, returnsTaskOrTaskOfInt);
            }

            if (method.Parameters.Length == 0)
            {
                return (true, returnsTaskOrTaskOfInt);
            }

            if (method.Parameters.Length > 1)
            {
                return (false, returnsTaskOrTaskOfInt);
            }

            if (!method.ParameterRefKinds.IsDefault)
            {
                return (false, returnsTaskOrTaskOfInt);
            }

            var firstType = method.Parameters[0].TypeWithAnnotations;
            if (firstType.TypeKind != TypeKind.Array)
            {
                return (false, returnsTaskOrTaskOfInt);
            }

            var array = (ArrayTypeSymbol)firstType.Type;
            return (array.IsSZArray && array.ElementType.SpecialType == SpecialType.System_String, returnsTaskOrTaskOfInt);
        }

        internal override bool IsUnreferencedAssemblyIdentityDiagnosticCode(int code)
            => code == (int)ErrorCode.ERR_NoTypeDef;

        internal class EntryPoint
        {
            public readonly MethodSymbol MethodSymbol;
            public readonly ImmutableArray<Diagnostic> Diagnostics;

            public EntryPoint(MethodSymbol methodSymbol, ImmutableArray<Diagnostic> diagnostics)
            {
                this.MethodSymbol = methodSymbol;
                this.Diagnostics = diagnostics;
            }
        }

        internal bool MightContainNoPiaLocalTypes()
        {
            return SourceAssembly.MightContainNoPiaLocalTypes();
        }

        // NOTE(cyrusn): There is a bit of a discoverability problem with this method and the same
        // named method in SyntaxTreeSemanticModel.  Technically, i believe these are the appropriate
        // locations for these methods.  This method has no dependencies on anything but the
        // compilation, while the other method needs a bindings object to determine what bound node
        // an expression syntax binds to.  Perhaps when we document these methods we should explain
        // where a user can find the other.
        /// <summary>
        /// Classifies a conversion from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Source type of value to be converted</param>
        /// <param name="destination">Destination type of value to be converted</param>
        /// <returns>A <see cref="Conversion"/> that classifies the conversion from the
        /// <paramref name="source"/> type to the <paramref name="destination"/> type.</returns>
        public Conversion ClassifyConversion(ITypeSymbol source, ITypeSymbol destination)
        {
            // Note that it is possible for there to be both an implicit user-defined conversion
            // and an explicit built-in conversion from source to destination. In that scenario
            // this method returns the implicit conversion.

            if ((object)source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if ((object)destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var cssource = source.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>(nameof(source));
            var csdest = destination.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>(nameof(destination));

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return Conversions.ClassifyConversionFromType(cssource, csdest, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Classifies a conversion from <paramref name="source"/> to <paramref name="destination"/> according
        /// to this compilation's programming language.
        /// </summary>
        /// <param name="source">Source type of value to be converted</param>
        /// <param name="destination">Destination type of value to be converted</param>
        /// <returns>A <see cref="CommonConversion"/> that classifies the conversion from the
        /// <paramref name="source"/> type to the <paramref name="destination"/> type.</returns>
        public override CommonConversion ClassifyCommonConversion(ITypeSymbol source, ITypeSymbol destination)
        {
            return ClassifyConversion(source, destination).ToCommonConversion();
        }

        internal override IConvertibleConversion ClassifyConvertibleConversion(IOperation source, ITypeSymbol destination, out Optional<object> constantValue)
        {
            constantValue = default;

            if (destination is null)
            {
                return Conversion.NoConversion;
            }

            ITypeSymbol sourceType = source.Type;

            if (sourceType is null)
            {
                if (source.ConstantValue.HasValue && source.ConstantValue.Value is null && destination.IsReferenceType)
                {
                    constantValue = source.ConstantValue;
                    return Conversion.NullLiteral;
                }

                return Conversion.NoConversion;
            }

            Conversion result = ClassifyConversion(sourceType, destination);

            if (result.IsReference && source.ConstantValue.HasValue && source.ConstantValue.Value is null)
            {
                constantValue = source.ConstantValue;
            }

            return result;
        }

        /// <summary>
        /// Returns a new ArrayTypeSymbol representing an array type tied to the base types of the
        /// COR Library in this Compilation.
        /// </summary>
        internal ArrayTypeSymbol CreateArrayTypeSymbol(TypeSymbol elementType, int rank = 1, NullableAnnotation elementNullableAnnotation = NullableAnnotation.Oblivious)
        {
            if ((object)elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (rank < 1)
            {
                throw new ArgumentException(nameof(rank));
            }

            return ArrayTypeSymbol.CreateCSharpArray(this.Assembly, TypeWithAnnotations.Create(elementType, elementNullableAnnotation), rank);
        }

        /// <summary>
        /// Returns a new PointerTypeSymbol representing a pointer type tied to a type in this Compilation.
        /// </summary>
        internal PointerTypeSymbol CreatePointerTypeSymbol(TypeSymbol elementType)
        {
            if ((object)elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            return new PointerTypeSymbol(TypeWithAnnotations.Create(elementType));
        }

        private protected override bool IsSymbolAccessibleWithinCore(
            ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughType)
        {
            var symbol0 = symbol.EnsureCSharpSymbolOrNull<ISymbol, Symbol>(nameof(symbol));
            var within0 = within.EnsureCSharpSymbolOrNull<ISymbol, Symbol>(nameof(within));
            var throughType0 = throughType.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>(nameof(throughType));
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            return
                within0.Kind == SymbolKind.Assembly ?
                AccessCheck.IsSymbolAccessible(symbol0, (AssemblySymbol)within0, ref useSiteDiagnostics) :
                AccessCheck.IsSymbolAccessible(symbol0, (NamedTypeSymbol)within0, ref useSiteDiagnostics, throughType0);
        }

        [Obsolete("Compilation.IsSymbolAccessibleWithin is not designed for use within the compilers", true)]
        internal new bool IsSymbolAccessibleWithin(
            ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughType = null)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Binding

        /// <summary>
        /// Gets a new SyntaxTreeSemanticModel for the specified syntax tree.
        /// </summary>
        public new SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            if (!_syntaxAndDeclarations.GetLazyState().RootNamespaces.ContainsKey(syntaxTree))
            {
                throw new ArgumentException(CSharpResources.SyntaxTreeNotFound, nameof(syntaxTree));
            }

            return new SyntaxTreeSemanticModel(this, (SyntaxTree)syntaxTree, ignoreAccessibility);
        }

        // When building symbols from the declaration table (lazily), or inside a type, or when
        // compiling a method body, we may not have a BinderContext in hand for the enclosing
        // scopes.  Therefore, we build them when needed (and cache them) using a ContextBuilder.
        // Since a ContextBuilder is only a cache, and the identity of the ContextBuilders and
        // BinderContexts have no semantic meaning, we can reuse them or rebuild them, whichever is
        // most convenient.  We store them using weak references so that GC pressure will cause them
        // to be recycled.
        private WeakReference<BinderFactory>[] _binderFactories;

        internal BinderFactory GetBinderFactory(SyntaxTree syntaxTree)
        {
            var treeNum = GetSyntaxTreeOrdinal(syntaxTree);
            var binderFactories = _binderFactories;
            if (binderFactories == null)
            {
                binderFactories = new WeakReference<BinderFactory>[this.SyntaxTrees.Length];
                binderFactories = Interlocked.CompareExchange(ref _binderFactories, binderFactories, null) ?? binderFactories;
            }

            BinderFactory previousFactory;
            var previousWeakReference = binderFactories[treeNum];
            if (previousWeakReference != null && previousWeakReference.TryGetTarget(out previousFactory))
            {
                return previousFactory;
            }

            return AddNewFactory(syntaxTree, ref binderFactories[treeNum]);
        }

        private BinderFactory AddNewFactory(SyntaxTree syntaxTree, ref WeakReference<BinderFactory> slot)
        {
            var newFactory = new BinderFactory(this, syntaxTree);
            var newWeakReference = new WeakReference<BinderFactory>(newFactory);

            while (true)
            {
                BinderFactory previousFactory;
                WeakReference<BinderFactory> previousWeakReference = slot;
                if (previousWeakReference != null && previousWeakReference.TryGetTarget(out previousFactory))
                {
                    return previousFactory;
                }

                if (Interlocked.CompareExchange(ref slot, newWeakReference, previousWeakReference) == previousWeakReference)
                {
                    return newFactory;
                }
            }
        }

        internal Binder GetBinder(CSharpSyntaxNode syntax)
        {
            return GetBinderFactory(syntax.SyntaxTree).GetBinder(syntax);
        }

        /// <summary>
        /// Returns imported symbols for the given declaration.
        /// </summary>
        internal Imports GetImports(SingleNamespaceDeclaration declaration)
        {
            return GetBinderFactory(declaration.SyntaxReference.SyntaxTree).GetImportsBinder((CSharpSyntaxNode)declaration.SyntaxReference.GetSyntax()).GetImports(basesBeingResolved: null);
        }

        private AliasSymbol CreateGlobalNamespaceAlias()
        {
            return AliasSymbol.CreateGlobalNamespaceAlias(this.GlobalNamespace, new InContainerBinder(this.GlobalNamespace, new BuckStopsHereBinder(this)));
        }

        private void CompleteTree(SyntaxTree tree)
        {
            if (_lazyCompilationUnitCompletedTrees == null) Interlocked.CompareExchange(ref _lazyCompilationUnitCompletedTrees, new HashSet<SyntaxTree>(), null);
            lock (_lazyCompilationUnitCompletedTrees)
            {
                if (_lazyCompilationUnitCompletedTrees.Add(tree))
                {
                    // signal the end of the compilation unit
                    EventQueue.TryEnqueue(new CompilationUnitCompletedEvent(this, tree));

                    if (_lazyCompilationUnitCompletedTrees.Count == this.SyntaxTrees.Length)
                    {
                        // if that was the last tree, signal the end of compilation
                        CompleteCompilationEventQueue_NoLock();
                    }
                }
            }
        }

        internal override void ReportUnusedImports(SyntaxTree filterTree, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            if (_lazyImportInfos != null && filterTree?.Options.DocumentationMode != DocumentationMode.None)
            {
                foreach (ImportInfo info in _lazyImportInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SyntaxTree infoTree = info.Tree;
                    if ((filterTree == null || filterTree == infoTree) && infoTree.Options.DocumentationMode != DocumentationMode.None)
                    {
                        TextSpan infoSpan = info.Span;
                        if (!this.IsImportDirectiveUsed(infoTree, infoSpan.Start))
                        {
                            ErrorCode code = info.Kind == SyntaxKind.ExternAliasDirective
                                ? ErrorCode.HDN_UnusedExternAlias
                                : ErrorCode.HDN_UnusedUsingDirective;
                            diagnostics.Add(code, infoTree.GetLocation(infoSpan));
                        }
                    }
                }
            }

            CompleteTrees(filterTree);
        }

        internal override void CompleteTrees(SyntaxTree filterTree)
        {
            // By definition, a tree is complete when all of its compiler diagnostics have been reported.
            // Since unused imports are the last thing we compute and report, a tree is complete when
            // the unused imports have been reported.
            if (EventQueue != null)
            {
                if (filterTree != null)
                {
                    CompleteTree(filterTree);
                }
                else
                {
                    foreach (var tree in this.SyntaxTrees)
                    {
                        CompleteTree(tree);
                    }
                }
            }
        }

        internal void RecordImport(UsingDirectiveSyntax syntax)
        {
            RecordImportInternal(syntax);
        }

        internal void RecordImport(ExternAliasDirectiveSyntax syntax)
        {
            RecordImportInternal(syntax);
        }

        private void RecordImportInternal(CSharpSyntaxNode syntax)
        {
            LazyInitializer.EnsureInitialized(ref _lazyImportInfos).
                Add(new ImportInfo(syntax.SyntaxTree, syntax.Kind(), syntax.Span));
        }

        private struct ImportInfo : IEquatable<ImportInfo>
        {
            public readonly SyntaxTree Tree;
            public readonly SyntaxKind Kind;
            public readonly TextSpan Span;

            public ImportInfo(SyntaxTree tree, SyntaxKind kind, TextSpan span)
            {
                this.Tree = tree;
                this.Kind = kind;
                this.Span = span;
            }

            public override bool Equals(object obj)
            {
                return (obj is ImportInfo) && Equals((ImportInfo)obj);
            }

            public bool Equals(ImportInfo other)
            {
                return
                    other.Kind == this.Kind &&
                    other.Tree == this.Tree &&
                    other.Span == this.Span;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(Tree, Span.Start);
            }
        }

        #endregion

        #region Diagnostics

        internal override CommonMessageProvider MessageProvider
        {
            get { return _syntaxAndDeclarations.MessageProvider; }
        }

        /// <summary>
        /// The bag in which semantic analysis should deposit its diagnostics.
        /// </summary>
        internal DiagnosticBag DeclarationDiagnostics
        {
            get
            {
                // We should only be placing diagnostics in this bag until
                // we are done gathering declaration diagnostics. Assert that is
                // the case. But since we have bugs (see https://github.com/dotnet/roslyn/issues/846)
                // we disable the assertion until they are fixed.
                Debug.Assert(!_declarationDiagnosticsFrozen || true);
                if (_lazyDeclarationDiagnostics == null)
                {
                    var diagnostics = new DiagnosticBag();
                    Interlocked.CompareExchange(ref _lazyDeclarationDiagnostics, diagnostics, null);
                }

                return _lazyDeclarationDiagnostics;
            }
        }

        private DiagnosticBag _lazyDeclarationDiagnostics;
        private bool _declarationDiagnosticsFrozen;

        /// <summary>
        /// A bag in which diagnostics that should be reported after code gen can be deposited.
        /// </summary>
        internal DiagnosticBag AdditionalCodegenWarnings
        {
            get
            {
                return _additionalCodegenWarnings;
            }
        }

        private readonly DiagnosticBag _additionalCodegenWarnings = new DiagnosticBag();

        internal DeclarationTable Declarations
        {
            get
            {
                return _syntaxAndDeclarations.GetLazyState().DeclarationTable;
            }
        }

        internal MergedNamespaceDeclaration MergedRootDeclaration
        {
            get
            {
                return Declarations.GetMergedRoot(this);
            }
        }

        /// <summary>
        /// Gets the diagnostics produced during the parsing stage of a compilation. There are no diagnostics for declarations or accessor or
        /// method bodies, for example.
        /// </summary>
        public override ImmutableArray<Diagnostic> GetParseDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(CompilationStage.Parse, false, cancellationToken);
        }

        /// <summary>
        /// Gets the diagnostics produced during symbol declaration headers.  There are no diagnostics for accessor or
        /// method bodies, for example.
        /// </summary>
        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(CompilationStage.Declare, false, cancellationToken);
        }

        /// <summary>
        /// Gets the diagnostics produced during the analysis of method bodies and field initializers.
        /// </summary>
        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(CompilationStage.Compile, false, cancellationToken);
        }

        /// <summary>
        /// Gets the all the diagnostics for the compilation, including syntax, declaration, and binding. Does not
        /// include any diagnostics that might be produced during emit.
        /// </summary>
        public override ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnostics(DefaultDiagnosticsStage, true, cancellationToken);
        }

        internal ImmutableArray<Diagnostic> GetDiagnostics(CompilationStage stage, bool includeEarlierStages, CancellationToken cancellationToken)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            GetDiagnostics(stage, includeEarlierStages, diagnostics, cancellationToken);
            return diagnostics.ToReadOnlyAndFree();
        }

        internal override void GetDiagnostics(CompilationStage stage, bool includeEarlierStages, DiagnosticBag diagnostics, CancellationToken cancellationToken = default)
        {
            var builder = DiagnosticBag.GetInstance();

            if (stage == CompilationStage.Parse || (stage > CompilationStage.Parse && includeEarlierStages))
            {
                var syntaxTrees = this.SyntaxTrees;
                if (this.Options.ConcurrentBuild)
                {
                    var parallelOptions = cancellationToken.CanBeCanceled
                                        ? new ParallelOptions() { CancellationToken = cancellationToken }
                                        : DefaultParallelOptions;

                    Parallel.For(0, syntaxTrees.Length, parallelOptions,
                        UICultureUtilities.WithCurrentUICulture<int>(i =>
                        {
                            var syntaxTree = syntaxTrees[i];
                            AppendLoadDirectiveDiagnostics(builder, _syntaxAndDeclarations, syntaxTree);
                            builder.AddRange(syntaxTree.GetDiagnostics(cancellationToken));
                        }));
                }
                else
                {
                    foreach (var syntaxTree in syntaxTrees)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        AppendLoadDirectiveDiagnostics(builder, _syntaxAndDeclarations, syntaxTree);

                        cancellationToken.ThrowIfCancellationRequested();
                        builder.AddRange(syntaxTree.GetDiagnostics(cancellationToken));
                    }
                }

                var parseOptionsReported = new HashSet<ParseOptions>();
                foreach (var syntaxTree in syntaxTrees)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!syntaxTree.Options.Errors.IsDefaultOrEmpty && parseOptionsReported.Add(syntaxTree.Options))
                    {
                        var location = syntaxTree.GetLocation(TextSpan.FromBounds(0, 0));
                        foreach (var error in syntaxTree.Options.Errors)
                        {
                            builder.Add(error.WithLocation(location));
                        }
                    }
                }
            }

            if (stage == CompilationStage.Declare || stage > CompilationStage.Declare && includeEarlierStages)
            {
                CheckAssemblyName(builder);
                builder.AddRange(Options.Errors);

                if (Options.NullableContextOptions != NullableContextOptions.Disable && LanguageVersion < MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion() &&
                    _syntaxAndDeclarations.ExternalSyntaxTrees.Any())
                {
                    builder.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_NullableOptionNotAvailable,
                                                 nameof(Options.NullableContextOptions), Options.NullableContextOptions, LanguageVersion.ToDisplayString(),
                                                 new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion())), Location.None));
                }

                cancellationToken.ThrowIfCancellationRequested();

                // the set of diagnostics related to establishing references.
                builder.AddRange(GetBoundReferenceManager().Diagnostics);

                cancellationToken.ThrowIfCancellationRequested();

                builder.AddRange(GetSourceDeclarationDiagnostics(cancellationToken: cancellationToken));

                if (EventQueue != null && SyntaxTrees.Length == 0)
                {
                    EnsureCompilationEventQueueCompleted();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (stage == CompilationStage.Compile || stage > CompilationStage.Compile && includeEarlierStages)
            {
                var methodBodyDiagnostics = DiagnosticBag.GetInstance();
                GetDiagnosticsForAllMethodBodies(methodBodyDiagnostics, cancellationToken);
                builder.AddRangeAndFree(methodBodyDiagnostics);
            }

            // Before returning diagnostics, we filter warnings
            // to honor the compiler options (e.g., /nowarn, /warnaserror and /warn) and the pragmas.
            FilterAndAppendAndFreeDiagnostics(diagnostics, ref builder);
        }

        private static void AppendLoadDirectiveDiagnostics(DiagnosticBag builder, SyntaxAndDeclarationManager syntaxAndDeclarations, SyntaxTree syntaxTree, Func<IEnumerable<Diagnostic>, IEnumerable<Diagnostic>> locationFilterOpt = null)
        {
            ImmutableArray<LoadDirective> loadDirectives;
            if (syntaxAndDeclarations.GetLazyState().LoadDirectiveMap.TryGetValue(syntaxTree, out loadDirectives))
            {
                Debug.Assert(!loadDirectives.IsEmpty);
                foreach (var directive in loadDirectives)
                {
                    IEnumerable<Diagnostic> diagnostics = directive.Diagnostics;
                    if (locationFilterOpt != null)
                    {
                        diagnostics = locationFilterOpt(diagnostics);
                    }
                    builder.AddRange(diagnostics);
                }
            }
        }

        // Do the steps in compilation to get the method body diagnostics, but don't actually generate
        // IL or emit an assembly.
        private void GetDiagnosticsForAllMethodBodies(DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            MethodCompiler.CompileMethodBodies(
                compilation: this,
                moduleBeingBuiltOpt: null,
                emittingPdb: false,
                emitTestCoverageData: false,
                hasDeclarationErrors: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: cancellationToken);

            DocumentationCommentCompiler.WriteDocumentationCommentXml(this, null, null, diagnostics, cancellationToken);
            this.ReportUnusedImports(null, diagnostics, cancellationToken);
        }

        private static bool IsDefinedOrImplementedInSourceTree(Symbol symbol, SyntaxTree tree, TextSpan? span)
        {
            if (symbol.IsDefinedInSourceTree(tree, span))
            {
                return true;
            }

            if (symbol.Kind == SymbolKind.Method && symbol.IsImplicitlyDeclared && ((MethodSymbol)symbol).MethodKind == MethodKind.Constructor)
            {
                // Include implicitly declared constructor if containing type is included
                return IsDefinedOrImplementedInSourceTree(symbol.ContainingType, tree, span);
            }

            return false;
        }

        private ImmutableArray<Diagnostic> GetDiagnosticsForMethodBodiesInTree(SyntaxTree tree, TextSpan? span, CancellationToken cancellationToken)
        {
            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();

            MethodCompiler.CompileMethodBodies(
                compilation: this,
                moduleBeingBuiltOpt: null,
                emittingPdb: false,
                emitTestCoverageData: false,
                hasDeclarationErrors: false,
                diagnostics: diagnostics,
                filterOpt: s => IsDefinedOrImplementedInSourceTree(s, tree, span),
                cancellationToken: cancellationToken);

            DocumentationCommentCompiler.WriteDocumentationCommentXml(this, null, null, diagnostics, cancellationToken, tree, span);

            // Report unused directives only if computing diagnostics for the entire tree.
            // Otherwise we cannot determine if a particular directive is used outside of the given sub-span within the tree.
            if (!span.HasValue || span.Value == tree.GetRoot(cancellationToken).FullSpan)
            {
                ReportUnusedImports(tree, diagnostics, cancellationToken);
            }

            return diagnostics.ToReadOnlyAndFree();
        }

        private ImmutableArray<Diagnostic> GetSourceDeclarationDiagnostics(SyntaxTree syntaxTree = null, TextSpan? filterSpanWithinTree = null, Func<IEnumerable<Diagnostic>, SyntaxTree, TextSpan?, IEnumerable<Diagnostic>> locationFilterOpt = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            GlobalImports.Complete(cancellationToken);

            SourceLocation location = null;
            if (syntaxTree != null)
            {
                var root = syntaxTree.GetRoot(cancellationToken);
                location = filterSpanWithinTree.HasValue ?
                    new SourceLocation(syntaxTree, filterSpanWithinTree.Value) :
                    new SourceLocation(root);
            }

            Assembly.ForceComplete(location, cancellationToken);

            if (syntaxTree is null)
            {
                // Don't freeze the compilation if we're getting
                // diagnostics for a single tree
                _declarationDiagnosticsFrozen = true;

                // Also freeze generated attribute flags.
                // Symbols bound after getting the declaration
                // diagnostics shouldn't need to modify the flags.
                _needsGeneratedAttributes_IsFrozen = true;
            }

            var result = _lazyDeclarationDiagnostics?.AsEnumerable() ?? Enumerable.Empty<Diagnostic>();

            if (locationFilterOpt != null)
            {
                Debug.Assert(syntaxTree != null);
                result = locationFilterOpt(result, syntaxTree, filterSpanWithinTree);
            }

            // NOTE: Concatenate the CLS diagnostics *after* filtering by tree/span, because they're already filtered.
            ImmutableArray<Diagnostic> clsDiagnostics = GetClsComplianceDiagnostics(syntaxTree, filterSpanWithinTree, cancellationToken);

            return result.AsImmutable().Concat(clsDiagnostics);
        }

        private ImmutableArray<Diagnostic> GetClsComplianceDiagnostics(SyntaxTree syntaxTree, TextSpan? filterSpanWithinTree, CancellationToken cancellationToken)
        {
            if (syntaxTree != null)
            {
                var builder = DiagnosticBag.GetInstance();
                ClsComplianceChecker.CheckCompliance(this, builder, cancellationToken, syntaxTree, filterSpanWithinTree);
                return builder.ToReadOnlyAndFree();
            }

            if (_lazyClsComplianceDiagnostics.IsDefault)
            {
                var builder = DiagnosticBag.GetInstance();
                ClsComplianceChecker.CheckCompliance(this, builder, cancellationToken);
                ImmutableInterlocked.InterlockedInitialize(ref _lazyClsComplianceDiagnostics, builder.ToReadOnlyAndFree());
            }

            Debug.Assert(!_lazyClsComplianceDiagnostics.IsDefault);
            return _lazyClsComplianceDiagnostics;
        }

        private static IEnumerable<Diagnostic> FilterDiagnosticsByLocation(IEnumerable<Diagnostic> diagnostics, SyntaxTree tree, TextSpan? filterSpanWithinTree)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.HasIntersectingLocation(tree, filterSpanWithinTree))
                {
                    yield return diagnostic;
                }
            }
        }

        internal ImmutableArray<Diagnostic> GetDiagnosticsForSyntaxTree(
            CompilationStage stage,
            SyntaxTree syntaxTree,
            TextSpan? filterSpanWithinTree,
            bool includeEarlierStages,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var builder = DiagnosticBag.GetInstance();
            if (stage == CompilationStage.Parse || (stage > CompilationStage.Parse && includeEarlierStages))
            {
                AppendLoadDirectiveDiagnostics(builder, _syntaxAndDeclarations, syntaxTree,
                    diagnostics => FilterDiagnosticsByLocation(diagnostics, syntaxTree, filterSpanWithinTree));

                var syntaxDiagnostics = syntaxTree.GetDiagnostics();
                syntaxDiagnostics = FilterDiagnosticsByLocation(syntaxDiagnostics, syntaxTree, filterSpanWithinTree);
                builder.AddRange(syntaxDiagnostics);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (stage == CompilationStage.Declare || (stage > CompilationStage.Declare && includeEarlierStages))
            {
                var declarationDiagnostics = GetSourceDeclarationDiagnostics(syntaxTree, filterSpanWithinTree, FilterDiagnosticsByLocation, cancellationToken);
                // re-enabling/fixing the below assert is tracked by https://github.com/dotnet/roslyn/issues/21020
                // Debug.Assert(declarationDiagnostics.All(d => d.HasIntersectingLocation(syntaxTree, filterSpanWithinTree)));
                builder.AddRange(declarationDiagnostics);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (stage == CompilationStage.Compile || (stage > CompilationStage.Compile && includeEarlierStages))
            {
                //remove some errors that don't have locations in the tree, like "no suitable main method."
                //Members in trees other than the one being examined are not compiled. This includes field
                //initializers which can result in 'field is never initialized' warnings for fields in partial
                //types when the field is in a different source file than the one for which we're getting diagnostics.
                //For that reason the bag must be also filtered by tree.
                IEnumerable<Diagnostic> methodBodyDiagnostics = GetDiagnosticsForMethodBodiesInTree(syntaxTree, filterSpanWithinTree, cancellationToken);

                // TODO: Enable the below commented assert and remove the filtering code in the next line.
                //       GetDiagnosticsForMethodBodiesInTree seems to be returning diagnostics with locations that don't satisfy the filter tree/span, this must be fixed.
                // Debug.Assert(methodBodyDiagnostics.All(d => DiagnosticContainsLocation(d, syntaxTree, filterSpanWithinTree)));
                methodBodyDiagnostics = FilterDiagnosticsByLocation(methodBodyDiagnostics, syntaxTree, filterSpanWithinTree);

                builder.AddRange(methodBodyDiagnostics);
            }

            // Before returning diagnostics, we filter warnings
            // to honor the compiler options (/nowarn, /warnaserror and /warn) and the pragmas.
            var result = DiagnosticBag.GetInstance();
            FilterAndAppendAndFreeDiagnostics(result, ref builder);
            return result.ToReadOnlyAndFree<Diagnostic>();
        }

        #endregion

        #region Resources

        protected override void AppendDefaultVersionResource(Stream resourceStream)
        {
            var sourceAssembly = SourceAssembly;
            string fileVersion = sourceAssembly.FileVersion ?? sourceAssembly.Identity.Version.ToString();

            Win32ResourceConversions.AppendVersionToResourceStream(resourceStream,
                !this.Options.OutputKind.IsApplication(),
                fileVersion: fileVersion,
                originalFileName: this.SourceModule.Name,
                internalName: this.SourceModule.Name,
                productVersion: sourceAssembly.InformationalVersion ?? fileVersion,
                fileDescription: sourceAssembly.Title ?? " ", //alink would give this a blank if nothing was supplied.
                assemblyVersion: sourceAssembly.Identity.Version,
                legalCopyright: sourceAssembly.Copyright ?? " ", //alink would give this a blank if nothing was supplied.
                legalTrademarks: sourceAssembly.Trademark,
                productName: sourceAssembly.Product,
                comments: sourceAssembly.Description,
                companyName: sourceAssembly.Company);
        }

        #endregion

        #region Emit

        internal override byte LinkerMajorVersion => 0x30;

        internal override bool IsDelaySigned
        {
            get { return SourceAssembly.IsDelaySigned; }
        }

        internal override StrongNameKeys StrongNameKeys
        {
            get { return SourceAssembly.StrongNameKeys; }
        }

        internal override CommonPEModuleBuilder CreateModuleBuilder(
            EmitOptions emitOptions,
            IMethodSymbol debugEntryPoint,
            Stream sourceLinkStream,
            IEnumerable<EmbeddedText> embeddedTexts,
            IEnumerable<ResourceDescription> manifestResources,
            CompilationTestData testData,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!IsSubmission || HasCodeToEmit());

            string runtimeMDVersion = GetRuntimeMetadataVersion(emitOptions, diagnostics);
            if (runtimeMDVersion == null)
            {
                return null;
            }

            var moduleProps = ConstructModuleSerializationProperties(emitOptions, runtimeMDVersion);

            if (manifestResources == null)
            {
                manifestResources = SpecializedCollections.EmptyEnumerable<ResourceDescription>();
            }

            PEModuleBuilder moduleBeingBuilt;
            if (_options.OutputKind.IsNetModule())
            {
                moduleBeingBuilt = new PENetModuleBuilder(
                    (SourceModuleSymbol)SourceModule,
                    emitOptions,
                    moduleProps,
                    manifestResources);
            }
            else
            {
                var kind = _options.OutputKind.IsValid() ? _options.OutputKind : OutputKind.DynamicallyLinkedLibrary;
                moduleBeingBuilt = new PEAssemblyBuilder(
                    SourceAssembly,
                    emitOptions,
                    kind,
                    moduleProps,
                    manifestResources);
            }

            if (debugEntryPoint != null)
            {
                moduleBeingBuilt.SetDebugEntryPoint((MethodSymbol)debugEntryPoint, diagnostics);
            }

            moduleBeingBuilt.SourceLinkStreamOpt = sourceLinkStream;

            if (embeddedTexts != null)
            {
                moduleBeingBuilt.EmbeddedTexts = embeddedTexts;
            }

            // testData is only passed when running tests.
            if (testData != null)
            {
                moduleBeingBuilt.SetMethodTestData(testData.Methods);
                testData.Module = moduleBeingBuilt;
            }

            return moduleBeingBuilt;
        }

        internal override bool CompileMethods(
            CommonPEModuleBuilder moduleBuilder,
            bool emittingPdb,
            bool emitMetadataOnly,
            bool emitTestCoverageData,
            DiagnosticBag diagnostics,
            Predicate<ISymbol> filterOpt,
            CancellationToken cancellationToken)
        {
            // The diagnostics should include syntax and declaration errors. We insert these before calling Emitter.Emit, so that the emitter
            // does not attempt to emit if there are declaration errors (but we do insert all errors from method body binding...)
            PooledHashSet<int> excludeDiagnostics = null;
            if (emitMetadataOnly)
            {
                excludeDiagnostics = PooledHashSet<int>.GetInstance();
                excludeDiagnostics.Add((int)ErrorCode.ERR_ConcreteMissingBody);
            }
            bool hasDeclarationErrors = !FilterAndAppendDiagnostics(diagnostics, GetDiagnostics(CompilationStage.Declare, true, cancellationToken), excludeDiagnostics);
            excludeDiagnostics?.Free();

            // TODO (tomat): NoPIA:
            // EmbeddedSymbolManager.MarkAllDeferredSymbolsAsReferenced(this)

            var moduleBeingBuilt = (PEModuleBuilder)moduleBuilder;

            if (emitMetadataOnly)
            {
                if (hasDeclarationErrors)
                {
                    return false;
                }

                if (moduleBeingBuilt.SourceModule.HasBadAttributes)
                {
                    // If there were errors but no declaration diagnostics, explicitly add a "Failed to emit module" error.
                    diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, NoLocation.Singleton, ((Cci.INamedEntity)moduleBeingBuilt).Name);
                    return false;
                }

                SynthesizedMetadataCompiler.ProcessSynthesizedMembers(this, moduleBeingBuilt, cancellationToken);
            }
            else
            {
                if ((emittingPdb || emitTestCoverageData) &&
                    !CreateDebugDocuments(moduleBeingBuilt.DebugDocumentsBuilder, moduleBeingBuilt.EmbeddedTexts, diagnostics))
                {
                    return false;
                }

                // Perform initial bind of method bodies in spite of earlier errors. This is the same
                // behavior as when calling GetDiagnostics()

                // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
                DiagnosticBag methodBodyDiagnosticBag = DiagnosticBag.GetInstance();

                MethodCompiler.CompileMethodBodies(
                    this,
                    moduleBeingBuilt,
                    emittingPdb,
                    emitTestCoverageData,
                    hasDeclarationErrors,
                    diagnostics: methodBodyDiagnosticBag,
                    filterOpt: filterOpt,
                    cancellationToken: cancellationToken);

                bool hasMethodBodyError = !FilterAndAppendAndFreeDiagnostics(diagnostics, ref methodBodyDiagnosticBag);

                if (hasDeclarationErrors || hasMethodBodyError)
                {
                    return false;
                }
            }

            return true;
        }

        internal override bool GenerateResourcesAndDocumentationComments(
            CommonPEModuleBuilder moduleBuilder,
            Stream xmlDocStream,
            Stream win32Resources,
            string outputNameOverride,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            var resourceDiagnostics = DiagnosticBag.GetInstance();

            SetupWin32Resources(moduleBuilder, win32Resources, resourceDiagnostics);

            ReportManifestResourceDuplicates(
                moduleBuilder.ManifestResources,
                SourceAssembly.Modules.Skip(1).Select(m => m.Name),   //all modules except the first one
                AddedModulesResourceNames(resourceDiagnostics),
                resourceDiagnostics);

            if (!FilterAndAppendAndFreeDiagnostics(diagnostics, ref resourceDiagnostics))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            var xmlDiagnostics = DiagnosticBag.GetInstance();

            string assemblyName = FileNameUtilities.ChangeExtension(outputNameOverride, extension: null);
            DocumentationCommentCompiler.WriteDocumentationCommentXml(this, assemblyName, xmlDocStream, xmlDiagnostics, cancellationToken);

            return FilterAndAppendAndFreeDiagnostics(diagnostics, ref xmlDiagnostics);
        }

        private IEnumerable<string> AddedModulesResourceNames(DiagnosticBag diagnostics)
        {
            ImmutableArray<ModuleSymbol> modules = SourceAssembly.Modules;

            for (int i = 1; i < modules.Length; i++)
            {
                var m = (Symbols.Metadata.PE.PEModuleSymbol)modules[i];
                ImmutableArray<EmbeddedResource> resources;

                try
                {
                    resources = m.Module.GetEmbeddedResourcesOrThrow();
                }
                catch (BadImageFormatException)
                {
                    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, m), NoLocation.Singleton);
                    continue;
                }

                foreach (var resource in resources)
                {
                    yield return resource.Name;
                }
            }
        }

        internal override EmitDifferenceResult EmitDifference(
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Func<ISymbol, bool> isAddedSymbol,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<MethodDefinitionHandle> updatedMethods,
            CompilationTestData testData,
            CancellationToken cancellationToken)
        {
            return EmitHelpers.EmitDifference(
                this,
                baseline,
                edits,
                isAddedSymbol,
                metadataStream,
                ilStream,
                pdbStream,
                updatedMethods,
                testData,
                cancellationToken);
        }

        internal string GetRuntimeMetadataVersion(EmitOptions emitOptions, DiagnosticBag diagnostics)
        {
            string runtimeMDVersion = GetRuntimeMetadataVersion(emitOptions);
            if (runtimeMDVersion != null)
            {
                return runtimeMDVersion;
            }

            DiagnosticBag runtimeMDVersionDiagnostics = DiagnosticBag.GetInstance();
            runtimeMDVersionDiagnostics.Add(ErrorCode.WRN_NoRuntimeMetadataVersion, NoLocation.Singleton);
            if (!FilterAndAppendAndFreeDiagnostics(diagnostics, ref runtimeMDVersionDiagnostics))
            {
                return null;
            }

            return string.Empty; //prevent emitter from crashing.
        }

        private string GetRuntimeMetadataVersion(EmitOptions emitOptions)
        {
            var corAssembly = Assembly.CorLibrary as Symbols.Metadata.PE.PEAssemblySymbol;

            if ((object)corAssembly != null)
            {
                return corAssembly.Assembly.ManifestModule.MetadataVersion;
            }

            return emitOptions.RuntimeMetadataVersion;
        }

        internal override void AddDebugSourceDocumentsForChecksumDirectives(
            DebugDocumentsBuilder documentsBuilder,
            SyntaxTree tree,
            DiagnosticBag diagnostics)
        {
            var checksumDirectives = tree.GetRoot().GetDirectives(d => d.Kind() == SyntaxKind.PragmaChecksumDirectiveTrivia &&
                                                                 !d.ContainsDiagnostics);

            foreach (var directive in checksumDirectives)
            {
                var checksumDirective = (PragmaChecksumDirectiveTriviaSyntax)directive;
                var path = checksumDirective.File.ValueText;

                var checksumText = checksumDirective.Bytes.ValueText;
                var normalizedPath = documentsBuilder.NormalizeDebugDocumentPath(path, basePath: tree.FilePath);
                var existingDoc = documentsBuilder.TryGetDebugDocumentForNormalizedPath(normalizedPath);

                // duplicate checksum pragmas are valid as long as values match
                // if we have seen this document already, check for matching values.
                if (existingDoc != null)
                {
                    // pragma matches a file path on an actual tree.
                    // Dev12 compiler just ignores the pragma in this case which means that
                    // checksum of the actual tree always wins and no warning is given.
                    // We will continue doing the same.
                    if (existingDoc.IsComputedChecksum)
                    {
                        continue;
                    }

                    var sourceInfo = existingDoc.GetSourceInfo();
                    if (ChecksumMatches(checksumText, sourceInfo.Checksum))
                    {
                        var guid = Guid.Parse(checksumDirective.Guid.ValueText);
                        if (guid == sourceInfo.ChecksumAlgorithmId)
                        {
                            // all parts match, nothing to do
                            continue;
                        }
                    }

                    // did not match to an existing document
                    // produce a warning and ignore the pragma
                    diagnostics.Add(ErrorCode.WRN_ConflictingChecksum, new SourceLocation(checksumDirective), path);
                }
                else
                {
                    var newDocument = new Cci.DebugSourceDocument(
                        normalizedPath,
                        Cci.DebugSourceDocument.CorSymLanguageTypeCSharp,
                        MakeChecksumBytes(checksumDirective.Bytes.ValueText),
                        Guid.Parse(checksumDirective.Guid.ValueText));

                    documentsBuilder.AddDebugDocument(newDocument);
                }
            }
        }

        private static bool ChecksumMatches(string bytesText, ImmutableArray<byte> bytes)
        {
            if (bytesText.Length != bytes.Length * 2)
            {
                return false;
            }

            for (int i = 0, len = bytesText.Length / 2; i < len; i++)
            {
                // 1A  in text becomes   0x1A
                var b = SyntaxFacts.HexValue(bytesText[i * 2]) * 16 +
                        SyntaxFacts.HexValue(bytesText[i * 2 + 1]);

                if (b != bytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static ImmutableArray<byte> MakeChecksumBytes(string bytesText)
        {
            int length = bytesText.Length / 2;
            var builder = ArrayBuilder<byte>.GetInstance(length);

            for (int i = 0; i < length; i++)
            {
                // 1A  in text becomes   0x1A
                var b = SyntaxFacts.HexValue(bytesText[i * 2]) * 16 +
                        SyntaxFacts.HexValue(bytesText[i * 2 + 1]);

                builder.Add((byte)b);
            }

            return builder.ToImmutableAndFree();
        }

        internal override Guid DebugSourceDocumentLanguageId => Cci.DebugSourceDocument.CorSymLanguageTypeCSharp;

        internal override bool HasCodeToEmit()
        {
            foreach (var syntaxTree in this.SyntaxTrees)
            {
                var unit = syntaxTree.GetCompilationUnitRoot();
                if (unit.Members.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Common Members

        protected override Compilation CommonWithReferences(IEnumerable<MetadataReference> newReferences)
        {
            return WithReferences(newReferences);
        }

        protected override Compilation CommonWithAssemblyName(string assemblyName)
        {
            return WithAssemblyName(assemblyName);
        }

        protected override IAssemblySymbol CommonAssembly
        {
            get { return this.Assembly; }
        }

        protected override INamespaceSymbol CommonGlobalNamespace
        {
            get { return this.GlobalNamespace; }
        }

        protected override CompilationOptions CommonOptions
        {
            get { return _options; }
        }

        protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility)
        {
            return this.GetSemanticModel((SyntaxTree)syntaxTree, ignoreAccessibility);
        }

        protected override IEnumerable<SyntaxTree> CommonSyntaxTrees
        {
            get
            {
                return this.SyntaxTrees;
            }
        }

        protected override Compilation CommonAddSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            return this.AddSyntaxTrees(trees);
        }

        protected override Compilation CommonRemoveSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            return this.RemoveSyntaxTrees(trees);
        }

        protected override Compilation CommonRemoveAllSyntaxTrees()
        {
            return this.RemoveAllSyntaxTrees();
        }

        protected override Compilation CommonReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
        {
            return this.ReplaceSyntaxTree((SyntaxTree)oldTree, (SyntaxTree)newTree);
        }

        protected override Compilation CommonWithOptions(CompilationOptions options)
        {
            return this.WithOptions((CSharpCompilationOptions)options);
        }

        protected override Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo info)
        {
            return this.WithScriptCompilationInfo((CSharpScriptCompilationInfo)info);
        }

        protected override bool CommonContainsSyntaxTree(SyntaxTree syntaxTree)
        {
            return this.ContainsSyntaxTree(syntaxTree);
        }

        protected override ISymbol CommonGetAssemblyOrModuleSymbol(MetadataReference reference)
        {
            return this.GetAssemblyOrModuleSymbol(reference);
        }

        protected override Compilation CommonClone()
        {
            return this.Clone();
        }

        protected override IModuleSymbol CommonSourceModule
        {
            get { return this.SourceModule; }
        }

        protected override INamedTypeSymbol CommonGetSpecialType(SpecialType specialType)
        {
            return this.GetSpecialType(specialType);
        }

        protected override INamespaceSymbol CommonGetCompilationNamespace(INamespaceSymbol namespaceSymbol)
        {
            return this.GetCompilationNamespace(namespaceSymbol);
        }

        protected override INamedTypeSymbol CommonGetTypeByMetadataName(string metadataName)
        {
            return this.GetTypeByMetadataName(metadataName);
        }

        protected override INamedTypeSymbol CommonScriptClass
        {
            get { return this.ScriptClass; }
        }

        protected override IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank, CodeAnalysis.NullableAnnotation elementNullableAnnotation)
        {
            return CreateArrayTypeSymbol(elementType.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>(nameof(elementType)), rank, elementNullableAnnotation.ToInternalAnnotation());
        }

        protected override IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType)
        {
            return CreatePointerTypeSymbol(elementType.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>(nameof(elementType)));
        }

        protected override INamedTypeSymbol CommonCreateTupleTypeSymbol(
            ImmutableArray<ITypeSymbol> elementTypes,
            ImmutableArray<string> elementNames,
            ImmutableArray<Location> elementLocations,
            ImmutableArray<CodeAnalysis.NullableAnnotation> elementNullableAnnotations)
        {
            var typesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(elementTypes.Length);
            for (int i = 0; i < elementTypes.Length; i++)
            {
                var elementType = elementTypes[i].EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>($"{nameof(elementTypes)}[{i}]");
                var annotation = elementNullableAnnotations.IsDefault ? NullableAnnotation.Oblivious : elementNullableAnnotations[i].ToInternalAnnotation();
                typesBuilder.Add(TypeWithAnnotations.Create(elementType, annotation));
            }

            return TupleTypeSymbol.Create(
                locationOpt: null, // no location for the type declaration
                elementTypesWithAnnotations: typesBuilder.ToImmutableAndFree(),
                elementLocations: elementLocations,
                elementNames: elementNames,
                compilation: this,
                shouldCheckConstraints: false,
                includeNullability: false,
                errorPositions: default(ImmutableArray<bool>));
        }

        protected override INamedTypeSymbol CommonCreateTupleTypeSymbol(
            INamedTypeSymbol underlyingType,
            ImmutableArray<string> elementNames,
            ImmutableArray<Location> elementLocations,
            ImmutableArray<CodeAnalysis.NullableAnnotation> elementNullableAnnotations)
        {
            var csharpUnderlyingTuple = underlyingType.EnsureCSharpSymbolOrNull<INamedTypeSymbol, NamedTypeSymbol>(nameof(underlyingType));

            int cardinality;
            if (!csharpUnderlyingTuple.IsTupleCompatible(out cardinality))
            {
                throw new ArgumentException(CodeAnalysisResources.TupleUnderlyingTypeMustBeTupleCompatible, nameof(underlyingType));
            }

            elementNames = CheckTupleElementNames(cardinality, elementNames);
            CheckTupleElementLocations(cardinality, elementLocations);
            CheckTupleElementNullableAnnotations(cardinality, elementNullableAnnotations);

            var tupleType = TupleTypeSymbol.Create(
                csharpUnderlyingTuple, elementNames, elementLocations: elementLocations);
            if (!elementNullableAnnotations.IsDefault)
            {
                tupleType = tupleType.WithElementTypes(
                    tupleType.TupleElementTypesWithAnnotations.ZipAsArray(
                        elementNullableAnnotations,
                        (t, a) => TypeWithAnnotations.Create(t.Type, a.ToInternalAnnotation())));
            }
            return tupleType;
        }

        protected override INamedTypeSymbol CommonCreateAnonymousTypeSymbol(
            ImmutableArray<ITypeSymbol> memberTypes,
            ImmutableArray<string> memberNames,
            ImmutableArray<Location> memberLocations,
            ImmutableArray<bool> memberIsReadOnly,
            ImmutableArray<CodeAnalysis.NullableAnnotation> memberNullableAnnotations)
        {
            for (int i = 0, n = memberTypes.Length; i < n; i++)
            {
                memberTypes[i].EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>($"{nameof(memberTypes)}[{i}]");
            }

            if (!memberIsReadOnly.IsDefault && memberIsReadOnly.Any(v => !v))
            {
                throw new ArgumentException($"Non-ReadOnly members are not supported in C# anonymous types.");
            }

            var fields = ArrayBuilder<AnonymousTypeField>.GetInstance();

            for (int i = 0, n = memberTypes.Length; i < n; i++)
            {
                var type = memberTypes[i];
                var name = memberNames[i];
                var location = memberLocations.IsDefault ? Location.None : memberLocations[i];
                var nullableAnnotation = memberNullableAnnotations.IsDefault ? NullableAnnotation.Oblivious : memberNullableAnnotations[i].ToInternalAnnotation();
                fields.Add(new AnonymousTypeField(name, location, TypeWithAnnotations.Create((TypeSymbol)type, nullableAnnotation)));
            }

            var descriptor = new AnonymousTypeDescriptor(fields.ToImmutableAndFree(), Location.None);

            return this.AnonymousTypeManager.ConstructAnonymousTypeSymbol(descriptor);
        }

        protected override ITypeSymbol CommonDynamicType
        {
            get { return DynamicType; }
        }

        protected override INamedTypeSymbol CommonObjectType
        {
            get { return this.ObjectType; }
        }

        protected override IMethodSymbol CommonGetEntryPoint(CancellationToken cancellationToken)
        {
            return this.GetEntryPoint(cancellationToken);
        }

        internal override int CompareSourceLocations(Location loc1, Location loc2)
        {
            Debug.Assert(loc1.IsInSource);
            Debug.Assert(loc2.IsInSource);

            var comparison = CompareSyntaxTreeOrdering(loc1.SourceTree, loc2.SourceTree);
            if (comparison != 0)
            {
                return comparison;
            }

            return loc1.SourceSpan.Start - loc2.SourceSpan.Start;
        }

        internal override int CompareSourceLocations(SyntaxReference loc1, SyntaxReference loc2)
        {
            var comparison = CompareSyntaxTreeOrdering(loc1.SyntaxTree, loc2.SyntaxTree);
            if (comparison != 0)
            {
                return comparison;
            }

            return loc1.Span.Start - loc2.Span.Start;
        }

        /// <summary>
        /// Return true if there is a source declaration symbol name that meets given predicate.
        /// </summary>
        public override bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (filter == SymbolFilter.None)
            {
                throw new ArgumentException(CSharpResources.NoNoneSearchCriteria, nameof(filter));
            }

            return DeclarationTable.ContainsName(this.MergedRootDeclaration, predicate, filter, cancellationToken);
        }

        /// <summary>
        /// Return source declaration symbols whose name meets given predicate.
        /// </summary>
        public override IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (filter == SymbolFilter.None)
            {
                throw new ArgumentException(CSharpResources.NoNoneSearchCriteria, nameof(filter));
            }

            return new PredicateSymbolSearcher(this, filter, predicate, cancellationToken).GetSymbolsWithName();
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Return true if there is a source declaration symbol name that matches the provided name.
        /// This will be faster than <see cref="ContainsSymbolsWithName(Func{string, bool}, SymbolFilter, CancellationToken)"/>
        /// when predicate is just a simple string check.
        /// </summary>
        public override bool ContainsSymbolsWithName(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (filter == SymbolFilter.None)
            {
                throw new ArgumentException(CSharpResources.NoNoneSearchCriteria, nameof(filter));
            }

            return DeclarationTable.ContainsName(this.MergedRootDeclaration, name, filter, cancellationToken);
        }

        /// <summary>
        /// Return source declaration symbols whose name matches the provided name.  This will be
        /// faster than <see cref="GetSymbolsWithName(Func{string, bool}, SymbolFilter,
        /// CancellationToken)"/> when predicate is just a simple string check.  <paramref
        /// name="name"/> is case sensitive.
        /// </summary>
        public override IEnumerable<ISymbol> GetSymbolsWithName(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (filter == SymbolFilter.None)
            {
                throw new ArgumentException(CSharpResources.NoNoneSearchCriteria, nameof(filter));
            }

            return new NameSymbolSearcher(this, filter, name, cancellationToken).GetSymbolsWithName();
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        #endregion

        /// <summary>
        /// Returns if the compilation has all of the members necessary to emit metadata about
        /// dynamic types.
        /// </summary>
        /// <returns></returns>
        internal bool HasDynamicEmitAttributes()
        {
            return
                (object)GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctor) != null &&
                (object)GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags) != null;
        }

        internal bool HasTupleNamesAttributes =>
            (object)GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames) != null;

        /// <summary>
        /// Returns whether the compilation has the Boolean type and if it's good.
        /// </summary>
        /// <returns>Returns true if Boolean is present and healthy.</returns>
        internal bool CanEmitBoolean() => CanEmitSpecialType(SpecialType.System_Boolean);

        internal bool CanEmitSpecialType(SpecialType type)
        {
            var typeSymbol = GetSpecialType(type);
            var diagnostic = typeSymbol.GetUseSiteDiagnostic();
            return (diagnostic == null) || (diagnostic.Severity != DiagnosticSeverity.Error);
        }

        internal bool EmitNullablePublicOnly
        {
            get
            {
                if (!_lazyEmitNullablePublicOnly.HasValue())
                {
                    bool value = SyntaxTrees.FirstOrDefault()?.Options?.Features?.ContainsKey("nullablePublicOnly") == true;
                    _lazyEmitNullablePublicOnly = value.ToThreeState();
                }
                return _lazyEmitNullablePublicOnly.Value();
            }
        }

        internal bool ShouldEmitNullableAttributes(Symbol symbol)
        {
            Debug.Assert(symbol is object);
            Debug.Assert(symbol.IsDefinition);

            if (symbol.ContainingModule != SourceModule)
            {
                return false;
            }

            if (!EmitNullablePublicOnly)
            {
                return true;
            }

            // For symbols that do not have explicit accessibility in metadata,
            // use the accessibility of the container.
            symbol = getExplicitAccessibilitySymbol(symbol);

            if (!AccessCheck.IsEffectivelyPublicOrInternal(symbol, out bool isInternal))
            {
                return false;
            }

            return !isInternal || SourceAssembly.InternalsAreVisible;

            static Symbol getExplicitAccessibilitySymbol(Symbol symbol)
            {
                while (true)
                {
                    switch (symbol.Kind)
                    {
                        case SymbolKind.Parameter:
                        case SymbolKind.TypeParameter:
                        case SymbolKind.Property:
                        case SymbolKind.Event:
                            symbol = symbol.ContainingSymbol;
                            break;
                        default:
                            return symbol;
                    }
                }
            }
        }

        internal override AnalyzerDriver AnalyzerForLanguage(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager)
        {
            Func<SyntaxNode, SyntaxKind> getKind = node => node.Kind();
            Func<SyntaxTrivia, bool> isComment = trivia => trivia.Kind() == SyntaxKind.SingleLineCommentTrivia || trivia.Kind() == SyntaxKind.MultiLineCommentTrivia;
            return new AnalyzerDriver<SyntaxKind>(analyzers, getKind, analyzerManager, isComment);
        }

        internal void SymbolDeclaredEvent(Symbol symbol)
        {
            EventQueue?.TryEnqueue(new SymbolDeclaredCompilationEvent(this, symbol));
        }

        /// <summary>
        /// Determine if enum arrays can be initialized using block initialization.
        /// </summary>
        /// <returns>True if it's safe to use block initialization for enum arrays.</returns>
        /// <remarks>
        /// In NetFx 4.0, block array initializers do not work on all combinations of {32/64 X Debug/Retail} when array elements are enums.
        /// This is fixed in 4.5 thus enabling block array initialization for a very common case.
        /// We look for the presence of <see cref="System.Runtime.GCLatencyMode.SustainedLowLatency"/> which was introduced in .NET Framework 4.5
        /// </remarks>
        internal bool EnableEnumArrayBlockInitialization
        {
            get
            {
                var sustainedLowLatency = GetWellKnownTypeMember(WellKnownMember.System_Runtime_GCLatencyMode__SustainedLowLatency);
                return sustainedLowLatency != null && sustainedLowLatency.ContainingAssembly == Assembly.CorLibrary;
            }
        }

        private abstract class AbstractSymbolSearcher
        {
            private readonly PooledDictionary<Declaration, NamespaceOrTypeSymbol> _cache;
            private readonly CSharpCompilation _compilation;
            private readonly bool _includeNamespace;
            private readonly bool _includeType;
            private readonly bool _includeMember;
            private readonly CancellationToken _cancellationToken;

            protected AbstractSymbolSearcher(
                CSharpCompilation compilation, SymbolFilter filter, CancellationToken cancellationToken)
            {
                _cache = PooledDictionary<Declaration, NamespaceOrTypeSymbol>.GetInstance();

                _compilation = compilation;

                _includeNamespace = (filter & SymbolFilter.Namespace) == SymbolFilter.Namespace;
                _includeType = (filter & SymbolFilter.Type) == SymbolFilter.Type;
                _includeMember = (filter & SymbolFilter.Member) == SymbolFilter.Member;

                _cancellationToken = cancellationToken;
            }

            protected abstract bool Matches(string name);
            protected abstract bool ShouldCheckTypeForMembers(MergedTypeDeclaration current);

            public IEnumerable<ISymbol> GetSymbolsWithName()
            {
                var result = new HashSet<ISymbol>();
                var spine = ArrayBuilder<MergedNamespaceOrTypeDeclaration>.GetInstance();

                AppendSymbolsWithName(spine, _compilation.MergedRootDeclaration, result);

                spine.Free();
                _cache.Free();
                return result;
            }

            private void AppendSymbolsWithName(
                ArrayBuilder<MergedNamespaceOrTypeDeclaration> spine, MergedNamespaceOrTypeDeclaration current,
                HashSet<ISymbol> set)
            {
                if (current.Kind == DeclarationKind.Namespace)
                {
                    if (_includeNamespace && Matches(current.Name))
                    {
                        var container = GetSpineSymbol(spine);
                        var symbol = GetSymbol(container, current);
                        if (symbol != null)
                        {
                            set.Add(symbol);
                        }
                    }
                }
                else
                {
                    if (_includeType && Matches(current.Name))
                    {
                        var container = GetSpineSymbol(spine);
                        var symbol = GetSymbol(container, current);
                        if (symbol != null)
                        {
                            set.Add(symbol);
                        }
                    }

                    if (_includeMember)
                    {
                        var typeDeclaration = (MergedTypeDeclaration)current;
                        if (ShouldCheckTypeForMembers(typeDeclaration))
                        {
                            AppendMemberSymbolsWithName(spine, typeDeclaration, set);
                        }
                    }
                }

                spine.Add(current);

                foreach (var child in current.Children)
                {
                    if (child is MergedNamespaceOrTypeDeclaration mergedNamespaceOrType)
                    {
                        if (_includeMember || _includeType || child.Kind == DeclarationKind.Namespace)
                        {
                            AppendSymbolsWithName(spine, mergedNamespaceOrType, set);
                        }
                    }
                }

                // pop last one
                spine.RemoveAt(spine.Count - 1);
            }

            private void AppendMemberSymbolsWithName(
                ArrayBuilder<MergedNamespaceOrTypeDeclaration> spine, MergedTypeDeclaration current, HashSet<ISymbol> set)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                spine.Add(current);

                var container = GetSpineSymbol(spine);
                if (container != null)
                {
                    foreach (var member in container.GetMembers())
                    {
                        if (!member.IsTypeOrTypeAlias() &&
                            (member.CanBeReferencedByName || member.IsExplicitInterfaceImplementation() || member.IsIndexer()) &&
                            Matches(member.Name))
                        {
                            set.Add(member);
                        }
                    }
                }

                spine.RemoveAt(spine.Count - 1);
            }

            protected NamespaceOrTypeSymbol GetSpineSymbol(ArrayBuilder<MergedNamespaceOrTypeDeclaration> spine)
            {
                if (spine.Count == 0)
                {
                    return null;
                }

                var symbol = GetCachedSymbol(spine[spine.Count - 1]);
                if (symbol != null)
                {
                    return symbol;
                }

                NamespaceOrTypeSymbol current = _compilation.GlobalNamespace;
                for (var i = 1; i < spine.Count; i++)
                {
                    current = GetSymbol(current, spine[i]);
                }

                return current;
            }

            private NamespaceOrTypeSymbol GetCachedSymbol(MergedNamespaceOrTypeDeclaration declaration)
                => _cache.TryGetValue(declaration, out NamespaceOrTypeSymbol symbol)
                        ? symbol
                        : null;

            private NamespaceOrTypeSymbol GetSymbol(NamespaceOrTypeSymbol container, MergedNamespaceOrTypeDeclaration declaration)
            {
                if (container == null)
                {
                    return _compilation.GlobalNamespace;
                }

                if (declaration.Kind == DeclarationKind.Namespace)
                {
                    AddCache(container.GetMembers(declaration.Name).OfType<NamespaceOrTypeSymbol>());
                }
                else
                {
                    AddCache(container.GetTypeMembers(declaration.Name));
                }

                return GetCachedSymbol(declaration);
            }

            private void AddCache(IEnumerable<NamespaceOrTypeSymbol> symbols)
            {
                foreach (var symbol in symbols)
                {
                    var mergedNamespace = symbol as MergedNamespaceSymbol;
                    if (mergedNamespace != null)
                    {
                        _cache[mergedNamespace.ConstituentNamespaces.OfType<SourceNamespaceSymbol>().First().MergedDeclaration] = symbol;
                        continue;
                    }

                    var sourceNamespace = symbol as SourceNamespaceSymbol;
                    if (sourceNamespace != null)
                    {
                        _cache[sourceNamespace.MergedDeclaration] = sourceNamespace;
                        continue;
                    }

                    var sourceType = symbol as SourceMemberContainerTypeSymbol;
                    if ((object)sourceType != null)
                    {
                        _cache[sourceType.MergedDeclaration] = sourceType;
                    }
                }
            }
        }

        private class PredicateSymbolSearcher : AbstractSymbolSearcher
        {
            private readonly Func<string, bool> _predicate;

            public PredicateSymbolSearcher(
                CSharpCompilation compilation, SymbolFilter filter, Func<string, bool> predicate, CancellationToken cancellationToken)
                : base(compilation, filter, cancellationToken)
            {
                _predicate = predicate;
            }

            protected override bool ShouldCheckTypeForMembers(MergedTypeDeclaration current)
            {
                // Note: this preserves the behavior the compiler has always had when a predicate
                // is passed in.  We could potentially be smarter by checking the predicate
                // against the list of member names in the type declaration first.
                return true;
            }

            protected override bool Matches(string name)
                => _predicate(name);
        }

        private class NameSymbolSearcher : AbstractSymbolSearcher
        {
            private readonly string _name;

            public NameSymbolSearcher(
                CSharpCompilation compilation, SymbolFilter filter, string name, CancellationToken cancellationToken)
                : base(compilation, filter, cancellationToken)
            {
                _name = name;
            }

            protected override bool ShouldCheckTypeForMembers(MergedTypeDeclaration current)
            {
                foreach (SingleTypeDeclaration typeDecl in current.Declarations)
                {
                    if (typeDecl.MemberNames.Contains(_name))
                    {
                        return true;
                    }
                }

                return false;
            }

            protected override bool Matches(string name)
                => _name == name;
        }
    }
}

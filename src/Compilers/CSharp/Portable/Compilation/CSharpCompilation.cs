// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
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

        private readonly CSharpCompilationOptions _options;
        private UsingsFromOptionsAndDiagnostics? _lazyUsingsFromOptions;
        private ImmutableArray<NamespaceOrTypeAndUsingDirective> _lazyGlobalImports;
        private Imports? _lazyPreviousSubmissionImports;
        private AliasSymbol? _lazyGlobalNamespaceAlias;  // alias symbol used to resolve "global::".

        private NamedTypeSymbol? _lazyScriptClass = ErrorTypeSymbol.UnknownResultType;

        // The type of host object model if available.
        private TypeSymbol? _lazyHostObjectTypeSymbol;

        /// <summary>
        /// All imports (using directives and extern aliases) in syntax trees in this compilation.
        /// NOTE: We need to de-dup since the Imports objects that populate the list may be GC'd
        /// and re-created.
        /// Values are the sets of dependencies for corresponding directives.
        /// </summary>
        private ConcurrentDictionary<ImportInfo, ImmutableArray<AssemblySymbol>>? _lazyImportInfos;

        // Cache the CLS diagnostics for the whole compilation so they aren't computed repeatedly.
        // NOTE: Presently, we do not cache the per-tree diagnostics.
        private ImmutableArray<Diagnostic> _lazyClsComplianceDiagnostics;
        private ImmutableArray<AssemblySymbol> _lazyClsComplianceDependencies;

        private Conversions? _conversions;
        /// <summary>
        /// A conversions object that ignores nullability.
        /// </summary>
        internal Conversions Conversions
        {
            get
            {
                if (_conversions == null)
                {
                    Interlocked.CompareExchange(ref _conversions, new BuckStopsHereBinder(this, associatedFileIdentifier: null).Conversions, null);
                }

                return _conversions;
            }
        }

        /// <summary>
        /// Manages anonymous types declared in this compilation. Unifies types that are structurally equivalent.
        /// </summary>
        private AnonymousTypeManager? _lazyAnonymousTypeManager;

        private NamespaceSymbol? _lazyGlobalNamespace;

        private BuiltInOperators? _lazyBuiltInOperators;

        /// <summary>
        /// The <see cref="SourceAssemblySymbol"/> for this compilation. Do not access directly, use Assembly property
        /// instead. This field is lazily initialized by ReferenceManager, ReferenceManager.CacheLockObject must be locked
        /// while ReferenceManager "calculates" the value and assigns it, several threads must not perform duplicate
        /// "calculation" simultaneously.
        /// </summary>
        private SourceAssemblySymbol? _lazyAssemblySymbol;

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
        private EntryPoint? _lazyEntryPoint;

        /// <summary>
        /// Emit nullable attributes for only those members that are visible outside the assembly
        /// (public, protected, and if any [InternalsVisibleTo] attributes, internal members).
        /// If false, attributes are emitted for all members regardless of visibility.
        /// </summary>
        private ThreeState _lazyEmitNullablePublicOnly;

        /// <summary>
        /// The set of trees for which a <see cref="CompilationUnitCompletedEvent"/> has been added to the queue.
        /// </summary>
        private HashSet<SyntaxTree>? _lazyCompilationUnitCompletedTrees;

        /// <summary>
        /// The set of trees for which enough analysis was performed in order to record usage of using directives.
        /// Once all trees are processed the value is set to null.
        /// </summary>
        private ImmutableHashSet<SyntaxTree>? _usageOfUsingsRecordedInTrees = ImmutableHashSet<SyntaxTree>.Empty;

        internal ImmutableHashSet<SyntaxTree>? UsageOfUsingsRecordedInTrees => Volatile.Read(ref _usageOfUsingsRecordedInTrees);

        /// <summary>
        /// Cache of T to Nullable&lt;T&gt;.
        /// </summary>
        private ConcurrentCache<TypeSymbol, NamedTypeSymbol>? _lazyTypeToNullableVersion;

        /// <summary>Lazily caches SyntaxTrees by their mapped path. Used to look up the syntax tree referenced by an interceptor (temporary compat behavior).</summary>
        /// <remarks>Must be removed prior to interceptors stable release.</remarks>
        private ImmutableSegmentedDictionary<string, OneOrMany<SyntaxTree>> _mappedPathToSyntaxTree;

        /// <summary>Lazily caches SyntaxTrees by their path. Used to look up the syntax tree referenced by an interceptor.</summary>
        /// <remarks>Must be removed prior to interceptors stable release.</remarks>
        private ImmutableSegmentedDictionary<string, OneOrMany<SyntaxTree>> _pathToSyntaxTree;

        /// <summary>Lazily caches SyntaxTrees by their xxHash128 checksum. Used to look up the syntax tree referenced by an interceptor.</summary>
        private ImmutableSegmentedDictionary<ReadOnlyMemory<byte>, OneOrMany<SyntaxTree>> _contentHashToSyntaxTree;

        internal ExtendedErrorTypeSymbol ImplicitlyTypedVariableUsedInForbiddenZoneType
        {
            get
            {
                if (field is null)
                {
                    Interlocked.CompareExchange(ref field, new ExtendedErrorTypeSymbol(this, name: "var", arity: 0, errorInfo: null, variableUsedBeforeDeclaration: true), null);
                }

                return field;
            }
        }

        internal ExtendedErrorTypeSymbol ImplicitlyTypedVariableInferenceFailedType
        {
            get
            {
                if (field is null)
                {
                    Interlocked.CompareExchange(ref field, new ExtendedErrorTypeSymbol(this, name: "var", arity: 0, errorInfo: null, unreported: false), null);
                }

                return field;
            }
        }

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

        internal BuiltInOperators BuiltInOperators
        {
            get
            {
                return InterlockedOperations.Initialize(ref _lazyBuiltInOperators, static self => new BuiltInOperators(self), this);
            }
        }

        internal AnonymousTypeManager AnonymousTypeManager
        {
            get
            {
                return InterlockedOperations.Initialize(ref _lazyAnonymousTypeManager, static self => new AnonymousTypeManager(self), this);
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
        internal bool FeatureStrictEnabled => HasFeature(FeatureFlag.Strict);

        /// <summary>
        /// True when the "peverify-compat" feature flag is set or the language version is below C# 7.2.
        /// With this flag we will avoid certain patterns known not be compatible with PEVerify.
        /// The code may be less efficient and may deviate from spec in corner cases.
        /// The flag is only to be used if PEVerify pass is extremely important.
        /// </summary>
        internal bool IsPeVerifyCompatEnabled => LanguageVersion < LanguageVersion.CSharp7_2 || HasFeature(FeatureFlag.PEVerifyCompat);

        /// <summary>
        /// True when the "disable-length-based-switch" feature flag is set.
        /// When this flag is set, the compiler will not emit length-based switch for string dispatches.
        /// </summary>
        internal bool FeatureDisableLengthBasedSwitch => HasFeature(FeatureFlag.DisableLengthBasedSwitch);

        /// <summary>
        /// Returns true if nullable analysis is enabled in the text span represented by the syntax node.
        /// </summary>
        /// <remarks>
        /// This overload is used for member symbols during binding, or for cases other
        /// than symbols such as attribute arguments and parameter defaults.
        /// </remarks>
        internal bool IsNullableAnalysisEnabledIn(SyntaxNode syntax)
        {
            return IsNullableAnalysisEnabledIn((CSharpSyntaxTree)syntax.SyntaxTree, syntax.Span);
        }

        /// <summary>
        /// Returns true if nullable analysis is enabled in the text span.
        /// </summary>
        /// <remarks>
        /// This overload is used for member symbols during binding, or for cases other
        /// than symbols such as attribute arguments and parameter defaults.
        /// </remarks>
        internal bool IsNullableAnalysisEnabledIn(CSharpSyntaxTree tree, TextSpan span)
        {
            return GetNullableAnalysisValue() ??
                tree.IsNullableAnalysisEnabled(span) ??
                (Options.NullableContextOptions & NullableContextOptions.Warnings) != 0;
        }

        /// <summary>
        /// Returns true if nullable analysis is enabled for the method. For constructors, the
        /// region considered may include other constructors and field and property initializers.
        /// </summary>
        /// <remarks>
        /// This overload is intended for callers that rely on symbols rather than syntax. The overload
        /// uses the cached value calculated during binding (from potentially several spans)
        /// from <see cref="IsNullableAnalysisEnabledIn(CSharpSyntaxTree, TextSpan)"/>.
        /// </remarks>
        internal bool IsNullableAnalysisEnabledIn(MethodSymbol method)
        {
            return GetNullableAnalysisValue() ??
                method.IsNullableAnalysisEnabled();
        }

        /// <summary>
        /// Returns true if nullable analysis is enabled for all methods regardless
        /// of the actual nullable context.
        /// If this property returns true but IsNullableAnalysisEnabled returns false,
        /// any nullable analysis should be enabled but results should be ignored.
        /// </summary>
        /// <remarks>
        /// For DEBUG builds, we treat nullable analysis as enabled for all methods
        /// unless explicitly disabled, so that analysis is run, even though results may
        /// be ignored, to increase the chance of catching nullable regressions
        /// (e.g. https://github.com/dotnet/roslyn/issues/40136).
        /// </remarks>
        internal bool IsNullableAnalysisEnabledAlways
        {
            get
            {
                var value = GetNullableAnalysisValue();
#if DEBUG
                return value != false;
#else
                return value == true;
#endif
            }
        }

        /// <summary>
        /// Returns Feature("run-nullable-analysis") as a bool? value:
        /// true for "always"; false for "never"; and null otherwise.
        /// </summary>
        private bool? GetNullableAnalysisValue()
        {
            return Feature(FeatureFlag.RunNullableAnalysis) switch
            {
                "always" => true,
                "never" => false,
                _ => null,
            };
        }

        /// <summary>
        /// Returns true if this method should be processed with runtime async handling instead
        /// of compiler async state machine generation.
        /// </summary>
        internal bool IsRuntimeAsyncEnabledIn(Symbol? symbol)
        {
            if (!Assembly.RuntimeSupportsAsyncMethods)
            {
                return false;
            }

            if (symbol is not MethodSymbol method)
            {
                return false;
            }

            Debug.Assert(ReferenceEquals(method.ContainingAssembly, Assembly));

            var methodReturn = method.ReturnType.OriginalDefinition;
            if (((InternalSpecialType)methodReturn.ExtendedSpecialType) is not (
                    InternalSpecialType.System_Threading_Tasks_Task or
                    InternalSpecialType.System_Threading_Tasks_Task_T or
                    InternalSpecialType.System_Threading_Tasks_ValueTask or
                    InternalSpecialType.System_Threading_Tasks_ValueTask_T))
            {
                return false;
            }

            return symbol switch
            {
                SourceMethodSymbol { IsRuntimeAsyncEnabledInMethod: ThreeState.True } => true,
                SourceMethodSymbol { IsRuntimeAsyncEnabledInMethod: ThreeState.False } => false,
                _ => Feature(FeatureFlag.RuntimeAsync) == "on"
            };
        }

        /// <summary>
        /// The language version that was used to parse the syntax trees of this compilation.
        /// </summary>
        public LanguageVersion LanguageVersion
        {
            get;
        }

        protected override INamedTypeSymbol CommonCreateErrorTypeSymbol(INamespaceOrTypeSymbol? container, string name, int arity)
        {
            return new ExtendedErrorTypeSymbol(
                       container.EnsureCSharpSymbolOrNull(nameof(container)),
                       name, arity, errorInfo: null).GetPublicSymbol();
        }

        protected override INamespaceSymbol CommonCreateErrorNamespaceSymbol(INamespaceSymbol container, string name)
        {
            return new MissingNamespaceSymbol(
                       container.EnsureCSharpSymbolOrNull(nameof(container)),
                       name).GetPublicSymbol();
        }

        protected override IPreprocessingSymbol CommonCreatePreprocessingSymbol(string name)
        {
            return new Symbols.PublicModel.PreprocessingSymbol(name);
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
            string? assemblyName,
            IEnumerable<SyntaxTree>? syntaxTrees = null,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null)
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
            SyntaxTree? syntaxTree = null,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpCompilation? previousScriptCompilation = null,
            Type? returnType = null,
            Type? globalsType = null)
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
            string? assemblyName,
            CSharpCompilationOptions options,
            IEnumerable<SyntaxTree>? syntaxTrees,
            IEnumerable<MetadataReference>? references,
            CSharpCompilation? previousSubmission,
            Type? returnType,
            Type? hostObjectType,
            bool isSubmission)
        {
            RoslynDebug.Assert(options != null);
            Debug.Assert(!isSubmission || options.ReferencesSupersedeLowerVersions);

            var validatedReferences = ValidateReferences<CSharpCompilationReference>(references);

            // We can't reuse the whole Reference Manager entirely (reuseReferenceManager = false)
            // because the set of references of this submission differs from the previous one.
            // The submission inherits references of the previous submission, adds the previous submission reference
            // and may add more references passed explicitly or via #r.
            //
            // TODO: Consider reusing some results of the assembly binding to improve perf
            // since most of the binding work is similar.
            // https://github.com/dotnet/roslyn/issues/43397

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
                    state: null),
                semanticModelProvider: null);

            if (syntaxTrees != null)
            {
                compilation = compilation.AddSyntaxTrees(syntaxTrees);
            }

            Debug.Assert(compilation._lazyAssemblySymbol is null);
            return compilation;
        }

        private CSharpCompilation(
            string? assemblyName,
            CSharpCompilationOptions options,
            ImmutableArray<MetadataReference> references,
            CSharpCompilation? previousSubmission,
            Type? submissionReturnType,
            Type? hostObjectType,
            bool isSubmission,
            ReferenceManager? referenceManager,
            bool reuseReferenceManager,
            SyntaxAndDeclarationManager syntaxAndDeclarations,
            SemanticModelProvider? semanticModelProvider,
            AsyncQueue<CompilationEvent>? eventQueue = null)
            : this(assemblyName, options, references, previousSubmission, submissionReturnType, hostObjectType, isSubmission, referenceManager, reuseReferenceManager, syntaxAndDeclarations, SyntaxTreeCommonFeatures(syntaxAndDeclarations.ExternalSyntaxTrees), semanticModelProvider, eventQueue)
        {
        }

        private CSharpCompilation(
            string? assemblyName,
            CSharpCompilationOptions options,
            ImmutableArray<MetadataReference> references,
            CSharpCompilation? previousSubmission,
            Type? submissionReturnType,
            Type? hostObjectType,
            bool isSubmission,
            ReferenceManager? referenceManager,
            bool reuseReferenceManager,
            SyntaxAndDeclarationManager syntaxAndDeclarations,
            IReadOnlyDictionary<string, string> features,
            SemanticModelProvider? semanticModelProvider,
            AsyncQueue<CompilationEvent>? eventQueue = null)
            : base(assemblyName, references, features, isSubmission, semanticModelProvider, eventQueue)
        {
            _options = options;

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
                if (referenceManager is null)
                {
                    throw new ArgumentNullException(nameof(referenceManager));
                }

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

            Debug.Assert(_lazyAssemblySymbol is null);
            if (EventQueue != null) EventQueue.TryEnqueue(new CompilationStartedEvent(this));
        }

        internal override void ValidateDebugEntryPoint(IMethodSymbol debugEntryPoint, DiagnosticBag diagnostics)
        {
            Debug.Assert(debugEntryPoint != null);

            // Debug entry point has to be a method definition from this compilation.
            var methodSymbol = (debugEntryPoint as Symbols.PublicModel.MethodSymbol)?.UnderlyingMethodSymbol;
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
                _syntaxAndDeclarations,
                this.SemanticModelProvider);
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
                syntaxAndDeclarations,
                this.SemanticModelProvider);
        }

        /// <summary>
        /// Creates a new compilation with the specified name.
        /// </summary>
        public new CSharpCompilation WithAssemblyName(string? assemblyName)
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
                _syntaxAndDeclarations,
                this.SemanticModelProvider);
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
        public new CSharpCompilation WithReferences(IEnumerable<MetadataReference>? references)
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
                _syntaxAndDeclarations,
                this.SemanticModelProvider);
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
            bool reuseSyntaxAndDeclarationManager = oldOptions.ScriptClassName == options.ScriptClassName &&
                oldOptions.SourceReferenceResolver == options.SourceReferenceResolver;

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
                        state: null),
                this.SemanticModelProvider);
        }

        /// <summary>
        /// Returns a new compilation with the given compilation set as the previous submission.
        /// </summary>
        public CSharpCompilation WithScriptCompilationInfo(CSharpScriptCompilationInfo? info)
        {
            if (info == ScriptCompilationInfo)
            {
                return this;
            }

            // Metadata references are inherited from the previous submission,
            // so we can only reuse the manager if we can guarantee that these references are the same.
            // Check if the previous script compilation doesn't change.

            // TODO: Consider comparing the metadata references if they have been bound already.
            // https://github.com/dotnet/roslyn/issues/43397
            bool reuseReferenceManager = ReferenceEquals(ScriptCompilationInfo?.PreviousScriptCompilation, info?.PreviousScriptCompilation);

            return new CSharpCompilation(
                this.AssemblyName,
                _options,
                this.ExternalReferences,
                info?.PreviousScriptCompilation,
                info?.ReturnTypeOpt,
                info?.GlobalsType,
                isSubmission: info != null,
                _referenceManager,
                reuseReferenceManager,
                _syntaxAndDeclarations,
                this.SemanticModelProvider);
        }

        /// <summary>
        /// Returns a new compilation with the given semantic model provider.
        /// </summary>
        internal override Compilation WithSemanticModelProvider(SemanticModelProvider? semanticModelProvider)
        {
            if (this.SemanticModelProvider == semanticModelProvider)
            {
                return this;
            }

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
                _syntaxAndDeclarations,
                semanticModelProvider);
        }

        /// <summary>
        /// Returns a new compilation with a given event queue.
        /// </summary>
        internal override Compilation WithEventQueue(AsyncQueue<CompilationEvent>? eventQueue)
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
                _syntaxAndDeclarations,
                this.SemanticModelProvider,
                eventQueue);
        }

        #endregion

        #region Submission

        public new CSharpScriptCompilationInfo? ScriptCompilationInfo { get; }
        internal override ScriptCompilationInfo? CommonScriptCompilationInfo => ScriptCompilationInfo;

        internal CSharpCompilation? PreviousSubmission => ScriptCompilationInfo?.PreviousScriptCompilation;

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
            var lastGlobalStatement = (GlobalStatementSyntax?)root.Members.LastOrDefault(m => m.IsKind(SyntaxKind.GlobalStatement));
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
        public new bool ContainsSyntaxTree(SyntaxTree? syntaxTree)
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
        public new CSharpCompilation ReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree? newTree)
        {
            // this is just to force a cast exception
            oldTree = (CSharpSyntaxTree)oldTree;
            newTree = (CSharpSyntaxTree?)newTree;

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
            // https://github.com/dotnet/roslyn/issues/43397
            var reuseReferenceManager = !oldTree.HasReferenceOrLoadDirectives() && !newTree.HasReferenceOrLoadDirectives();
            syntaxAndDeclarations = syntaxAndDeclarations.ReplaceSyntaxTree(oldTree, newTree);

            return Update(_referenceManager, reuseReferenceManager, syntaxAndDeclarations);
        }

        internal override int GetSyntaxTreeOrdinal(SyntaxTree tree)
        {
            Debug.Assert(this.ContainsSyntaxTree(tree));
            try
            {
                return _syntaxAndDeclarations.GetLazyState().OrdinalMap[tree];
            }
            catch (KeyNotFoundException)
            {
                // Explicitly catching and re-throwing exception so we don't send the syntax
                // tree (potentially containing private user information) to telemetry.
                throw new KeyNotFoundException($"Syntax tree not found with file path: {tree.FilePath}");
            }
        }

        internal OneOrMany<SyntaxTree> GetSyntaxTreesByMappedPath(string mappedPath)
        {
            // This method supports a "compat" behavior for interceptor file path resolution.
            // It must be removed prior to stable release.

            // We could consider storing this on SyntaxAndDeclarationManager instead, and updating it incrementally.
            // However, this would make it more difficult for it to be "pay-for-play",
            // i.e. only created in compilations where interceptors are used.
            var mappedPathToSyntaxTree = _mappedPathToSyntaxTree;
            if (mappedPathToSyntaxTree.IsDefault)
            {
                RoslynImmutableInterlocked.InterlockedInitialize(ref _mappedPathToSyntaxTree, computeMappedPathToSyntaxTree());
                mappedPathToSyntaxTree = _mappedPathToSyntaxTree;
            }

            return mappedPathToSyntaxTree.TryGetValue(mappedPath, out var value) ? value : OneOrMany<SyntaxTree>.Empty;

            ImmutableSegmentedDictionary<string, OneOrMany<SyntaxTree>> computeMappedPathToSyntaxTree()
            {
                var builder = ImmutableSegmentedDictionary.CreateBuilder<string, OneOrMany<SyntaxTree>>();
                var resolver = Options.SourceReferenceResolver;
                foreach (var tree in SyntaxTrees)
                {
                    var path = resolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
                    builder[path] = builder.ContainsKey(path) ? builder[path].Add(tree) : OneOrMany.Create(tree);
                }
                return builder.ToImmutable();
            }
        }

        internal OneOrMany<SyntaxTree> GetSyntaxTreesByContentHash(ReadOnlyMemory<byte> contentHash)
        {
            Debug.Assert(contentHash.Length == InterceptableLocation1.ContentHashLength);

            var contentHashToSyntaxTree = _contentHashToSyntaxTree;
            if (contentHashToSyntaxTree.IsDefault)
            {
                RoslynImmutableInterlocked.InterlockedInitialize(ref _contentHashToSyntaxTree, computeHashToSyntaxTree());
                contentHashToSyntaxTree = _contentHashToSyntaxTree;
            }

            return contentHashToSyntaxTree.TryGetValue(contentHash, out var value) ? value : OneOrMany<SyntaxTree>.Empty;

            ImmutableSegmentedDictionary<ReadOnlyMemory<byte>, OneOrMany<SyntaxTree>> computeHashToSyntaxTree()
            {
                var builder = ImmutableSegmentedDictionary.CreateBuilder<ReadOnlyMemory<byte>, OneOrMany<SyntaxTree>>(ContentHashComparer.Instance);
                foreach (var tree in SyntaxTrees)
                {
                    var text = tree.GetText();
                    var hash = text.GetContentHash().AsMemory();
                    builder[hash] = builder.TryGetValue(hash, out var existing) ? existing.Add(tree) : OneOrMany.Create(tree);
                }
                return builder.ToImmutable();
            }
        }

        internal OneOrMany<SyntaxTree> GetSyntaxTreesByPath(string path)
        {
            // We could consider storing this on SyntaxAndDeclarationManager instead, and updating it incrementally.
            // However, this would make it more difficult for it to be "pay-for-play",
            // i.e. only created in compilations where interceptors are used.
            var pathToSyntaxTree = _pathToSyntaxTree;
            if (pathToSyntaxTree.IsDefault)
            {
                RoslynImmutableInterlocked.InterlockedInitialize(ref _pathToSyntaxTree, computePathToSyntaxTree());
                pathToSyntaxTree = _pathToSyntaxTree;
            }

            return pathToSyntaxTree.TryGetValue(path, out var value) ? value : OneOrMany<SyntaxTree>.Empty;

            ImmutableSegmentedDictionary<string, OneOrMany<SyntaxTree>> computePathToSyntaxTree()
            {
                var builder = ImmutableSegmentedDictionary.CreateBuilder<string, OneOrMany<SyntaxTree>>();
                foreach (var tree in SyntaxTrees)
                {
                    var path = FileUtilities.GetNormalizedPathOrOriginalPath(tree.FilePath, basePath: null);
                    builder[path] = builder.ContainsKey(path) ? builder[path].Add(tree) : OneOrMany.Create(tree);
                }
                return builder.ToImmutable();
            }
        }

        #endregion

        #region References

        internal override CommonReferenceManager CommonGetBoundReferenceManager()
        {
            return GetBoundReferenceManager();
        }

        internal new ReferenceManager GetBoundReferenceManager()
        {
            if (_lazyAssemblySymbol is null)
            {
                _referenceManager.CreateSourceAssemblyForCompilation(this);
                Debug.Assert(_lazyAssemblySymbol is object);
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
        internal new Symbol? GetAssemblyOrModuleSymbol(MetadataReference reference)
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

        internal override TSymbol? GetSymbolInternal<TSymbol>(ISymbol? symbol) where TSymbol : class
        {
            return (TSymbol?)(object?)symbol.GetSymbol<Symbol>();
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
        public MetadataReference? GetDirectiveReference(ReferenceDirectiveTriviaSyntax directive)
        {
            RoslynDebug.Assert(directive.SyntaxTree.FilePath is object);

            MetadataReference? reference;
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

        public override CompilationReference ToMetadataReference(ImmutableArray<string> aliases = default, bool embedInteropTypes = false)
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

            int length = referenceManager.ReferencedAssemblies.Length;

            assemblies.EnsureCapacity(assemblies.Count + length);

            for (int i = 0; i < length; i++)
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
        public new MetadataReference? GetMetadataReference(IAssemblySymbol assemblySymbol)
        {
            return base.GetMetadataReference(assemblySymbol);
        }

        private protected override MetadataReference? CommonGetMetadataReference(IAssemblySymbol assemblySymbol)
        {
            if (assemblySymbol is Symbols.PublicModel.AssemblySymbol { UnderlyingAssemblySymbol: var underlyingSymbol })
            {
                return GetMetadataReference(underlyingSymbol);
            }

            return null;
        }

        internal MetadataReference? GetMetadataReference(AssemblySymbol? assemblySymbol)
        {
            return GetBoundReferenceManager().GetMetadataReference(assemblySymbol);
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
                RoslynDebug.Assert(_lazyAssemblySymbol is object);
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
                if (_lazyGlobalNamespace is null)
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
        internal new NamespaceSymbol? GetCompilationNamespace(INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol is Symbols.PublicModel.NamespaceSymbol n &&
                namespaceSymbol.NamespaceKind == NamespaceKind.Compilation &&
                namespaceSymbol.ContainingCompilation == this)
            {
                return n.UnderlyingNamespaceSymbol;
            }

            var containingNamespace = namespaceSymbol.ContainingNamespace;
            if (containingNamespace == null)
            {
                return this.GlobalNamespace;
            }

            var current = GetCompilationNamespace(containingNamespace);
            if (current is object)
            {
                return current.GetNestedNamespace(namespaceSymbol.Name);
            }

            return null;
        }

        internal NamespaceSymbol? GetCompilationNamespace(NamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol.NamespaceKind == NamespaceKind.Compilation &&
                namespaceSymbol.ContainingCompilation == this)
            {
                return namespaceSymbol;
            }

            var containingNamespace = namespaceSymbol.ContainingNamespace;
            if (containingNamespace == null)
            {
                return this.GlobalNamespace;
            }

            var current = GetCompilationNamespace(containingNamespace);
            if (current is object)
            {
                return current.GetNestedNamespace(namespaceSymbol.Name);
            }

            return null;
        }

        private ConcurrentDictionary<string, NamespaceSymbol>? _externAliasTargets;

        internal bool GetExternAliasTarget(string aliasName, out NamespaceSymbol @namespace)
        {
            if (_externAliasTargets == null)
            {
                Interlocked.CompareExchange(ref _externAliasTargets, new ConcurrentDictionary<string, NamespaceSymbol>(), null);
            }
            else if (_externAliasTargets.TryGetValue(aliasName, out var cached))
            {
                @namespace = cached;
                return !(@namespace is MissingNamespaceSymbol);
            }

            ArrayBuilder<NamespaceSymbol>? builder = null;
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
                ? MergedNamespaceSymbol.Create(new NamespaceExtent(this), namespacesToMerge: builder!.ToImmutableAndFree(), containingNamespace: null, nameOpt: null)
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
        internal new NamedTypeSymbol? ScriptClass
        {
            get
            {
                if (ReferenceEquals(_lazyScriptClass, ErrorTypeSymbol.UnknownResultType))
                {
                    Interlocked.CompareExchange(ref _lazyScriptClass, BindScriptClass()!, ErrorTypeSymbol.UnknownResultType);
                }

                return _lazyScriptClass;
            }
        }

        /// <summary>
        /// Resolves a symbol that represents script container (Script class). Uses the
        /// full name of the container class stored in <see cref="CompilationOptions.ScriptClassName"/> to find the symbol.
        /// </summary>
        /// <returns>The Script class symbol or null if it is not defined.</returns>
        private ImplicitNamedTypeSymbol? BindScriptClass()
        {
            return (ImplicitNamedTypeSymbol?)CommonBindScriptClass().GetSymbol();
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
        internal ImmutableArray<NamespaceOrTypeAndUsingDirective> GlobalImports
            => InterlockedOperations.Initialize(ref _lazyGlobalImports, static self => self.BindGlobalImports(), arg: this);

        private ImmutableArray<NamespaceOrTypeAndUsingDirective> BindGlobalImports()
        {
            var usingsFromoptions = UsingsFromOptions;
            var previousSubmission = PreviousSubmission;
            var previousSubmissionImports = previousSubmission is object ? Imports.ExpandPreviousSubmissionImports(previousSubmission.GlobalImports, this) : ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty;

            if (usingsFromoptions.UsingNamespacesOrTypes.IsEmpty)
            {
                return previousSubmissionImports;
            }
            else if (previousSubmissionImports.IsEmpty)
            {
                return usingsFromoptions.UsingNamespacesOrTypes;
            }

            var boundUsings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
            var uniqueUsings = PooledHashSet<NamespaceOrTypeSymbol>.GetInstance();

            boundUsings.AddRange(usingsFromoptions.UsingNamespacesOrTypes);
            uniqueUsings.AddAll(usingsFromoptions.UsingNamespacesOrTypes.Select(static unt => unt.NamespaceOrType));

            foreach (var previousUsing in previousSubmissionImports)
            {
                if (uniqueUsings.Add(previousUsing.NamespaceOrType))
                {
                    boundUsings.Add(previousUsing);
                }
            }

            uniqueUsings.Free();

            return boundUsings.ToImmutableAndFree();
        }

        /// <summary>
        /// Global imports not including those from previous submissions.
        /// </summary>
        private UsingsFromOptionsAndDiagnostics UsingsFromOptions
            => InterlockedOperations.Initialize(ref _lazyUsingsFromOptions, static self => self.BindUsingsFromOptions(), this);

        private UsingsFromOptionsAndDiagnostics BindUsingsFromOptions() => UsingsFromOptionsAndDiagnostics.FromOptions(this);

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

            return ((SourceNamespaceSymbol)SourceModule.GlobalNamespace).GetImports((CSharpSyntaxNode)tree.GetRoot(), basesBeingResolved: null);
        }

        /// <summary>
        /// Imports from all previous submissions.
        /// </summary>
        internal Imports GetPreviousSubmissionImports()
            => InterlockedOperations.Initialize(ref _lazyPreviousSubmissionImports, static self => self.ExpandPreviousSubmissionImports(), this);

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
                return InterlockedOperations.Initialize(ref _lazyGlobalNamespaceAlias, static self => self.CreateGlobalNamespaceAlias(), this);
            }
        }

        /// <summary>
        /// Get the symbol for the predefined type from the COR Library referenced by this compilation.
        /// </summary>
        internal NamedTypeSymbol GetSpecialType(ExtendedSpecialType specialType)
        {
            if ((int)specialType <= (int)SpecialType.None || (int)specialType >= (int)InternalSpecialType.NextAvailable)
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

            Debug.Assert(result.ExtendedSpecialType == specialType);
            return result;
        }

        private ConcurrentCache<TypeSymbol, NamedTypeSymbol> TypeToNullableVersion
        {
            get
            {
                return InterlockedOperations.Initialize(ref _lazyTypeToNullableVersion, static () => new ConcurrentCache<TypeSymbol, NamedTypeSymbol>(size: 100));
            }
        }

        /// <summary>
        /// Given a provided <paramref name="typeArgument"/>, gives back <see cref="Nullable{T}"/> constructed with that
        /// argument.  This function is only intended to be used for very common instantiations produced heavily during
        /// binding.  Specifically, the nullable versions of enums, and the nullable versions of core built-ins.  So
        /// many of these are created that it's worthwhile to cache, keeping overall garbage low, while not ballooning
        /// the size of the cache itself.
        /// </summary>
        internal NamedTypeSymbol GetOrCreateNullableType(TypeSymbol typeArgument)
        {
#if DEBUG
            if (!isSupportedType(typeArgument))
                Debug.Fail($"Unsupported type argument: {typeArgument.ToDisplayString()}");
#endif

            var typeToNullableVersion = TypeToNullableVersion;
            if (!typeToNullableVersion.TryGetValue(typeArgument, out var constructedNullableInstance))
            {
                constructedNullableInstance = this.GetSpecialType(SpecialType.System_Nullable_T).Construct(typeArgument);
                typeToNullableVersion.TryAdd(typeArgument, constructedNullableInstance);
            }

            return constructedNullableInstance;

#if DEBUG
            static bool isSupportedType(TypeSymbol typeArgument)
            {
                if (typeArgument.IsEnumType())
                    return true;

                switch (typeArgument.SpecialType)
                {
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Char:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Boolean:
                        return true;
                }

                if (typeArgument.IsNativeIntegerType)
                    return true;

                return false;
            }
#endif
        }

        /// <summary>
        /// Get the symbol for the predefined type member from the COR Library referenced by this compilation.
        /// </summary>
        internal Symbol GetSpecialTypeMember(SpecialMember specialMember)
        {
            return Assembly.GetSpecialTypeMember(specialMember);
        }

        internal override ISymbolInternal CommonGetSpecialTypeMember(SpecialMember specialMember)
        {
            return GetSpecialTypeMember(specialMember);
        }

        internal TypeSymbol GetTypeByReflectionType(Type type, BindingDiagnosticBag diagnostics)
        {
            var result = Assembly.GetTypeByReflectionType(type);
            if (result is null)
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
                new object[] { type.AssemblyQualifiedName ?? "" },
                ImmutableArray<Symbol>.Empty,
                ImmutableArray<Location>.Empty
            );
        }

        protected override ITypeSymbol? CommonScriptGlobalsType
            => GetHostObjectTypeSymbol()?.GetPublicSymbol();

        internal TypeSymbol? GetHostObjectTypeSymbol()
        {
            if (HostObjectType != null && _lazyHostObjectTypeSymbol is null)
            {
                TypeSymbol? symbol = Assembly.GetTypeByReflectionType(HostObjectType);

                if (symbol is null)
                {
                    MetadataTypeName mdName = MetadataTypeName.FromNamespaceAndTypeName(HostObjectType.Namespace ?? String.Empty,
                                                                                        HostObjectType.Name,
                                                                                        useCLSCompliantNameArityEncoding: true);

                    symbol = new MissingMetadataTypeSymbol.TopLevel(
                        new MissingAssemblySymbol(AssemblyIdentity.FromAssemblyDefinition(HostObjectType.GetTypeInfo().Assembly)).Modules[0],
                        ref mdName,
                        SpecialType.None,
                        CreateReflectionTypeNotFoundError(HostObjectType));
                }

                Interlocked.CompareExchange(ref _lazyHostObjectTypeSymbol, symbol, null);
            }

            return _lazyHostObjectTypeSymbol;
        }

        internal SynthesizedInteractiveInitializerMethod? GetSubmissionInitializer()
        {
            return (IsSubmission && ScriptClass is object) ?
                ScriptClass.GetScriptInitializer() :
                null;
        }

        /// <summary>
        /// Gets the type within the compilation's assembly and all referenced assemblies (other than
        /// those that can only be referenced via an extern alias) using its canonical CLR metadata name.
        /// </summary>
        internal new NamedTypeSymbol? GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            var result = this.Assembly.GetTypeByMetadataName(fullyQualifiedMetadataName, includeReferences: true, isWellKnownType: false, conflicts: out var _);
            Debug.Assert(result?.IsErrorType() != true);
            return result;
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

        internal new MethodSymbol? GetEntryPoint(CancellationToken cancellationToken)
        {
            EntryPoint entryPoint = GetEntryPointAndDiagnostics(cancellationToken);
            return entryPoint.MethodSymbol;
        }

        internal EntryPoint GetEntryPointAndDiagnostics(CancellationToken cancellationToken)
        {
            if (_lazyEntryPoint == null)
            {
                EntryPoint? entryPoint;
                var simpleProgramEntryPointSymbol = SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(this);

                if (!this.Options.OutputKind.IsApplication() && (this.ScriptClass is null))
                {
                    if (simpleProgramEntryPointSymbol is object)
                    {
                        var diagnostics = BindingDiagnosticBag.GetInstance();
                        diagnostics.Add(ErrorCode.ERR_SimpleProgramNotAnExecutable, simpleProgramEntryPointSymbol.ReturnTypeSyntax.Location);
                        entryPoint = new EntryPoint(null, diagnostics.ToReadOnlyAndFree());
                    }
                    else
                    {
                        entryPoint = EntryPoint.None;
                    }
                }
                else
                {
                    entryPoint = null;

                    if (this.Options.MainTypeName != null && !this.Options.MainTypeName.IsValidClrTypeName())
                    {
                        Debug.Assert(!this.Options.Errors.IsDefaultOrEmpty);
                        entryPoint = EntryPoint.None;
                    }

                    if (entryPoint is null)
                    {
                        ReadOnlyBindingDiagnostic<AssemblySymbol> diagnostics;
                        var entryPointMethod = FindEntryPoint(simpleProgramEntryPointSymbol, cancellationToken, out diagnostics);
                        entryPoint = new EntryPoint(entryPointMethod, diagnostics);
                    }
                }

                Interlocked.CompareExchange(ref _lazyEntryPoint, entryPoint, null);
            }

            return _lazyEntryPoint;
        }

        private MethodSymbol? FindEntryPoint(MethodSymbol? simpleProgramEntryPointSymbol, CancellationToken cancellationToken, out ReadOnlyBindingDiagnostic<AssemblySymbol> sealedDiagnostics)
        {
            var diagnostics = BindingDiagnosticBag.GetInstance();
            RoslynDebug.Assert(diagnostics.DiagnosticBag is object);
            var entryPointCandidates = ArrayBuilder<MethodSymbol>.GetInstance();

            try
            {
                NamedTypeSymbol? mainType;

                string? mainTypeName = this.Options.MainTypeName;
                NamespaceSymbol globalNamespace = this.SourceModule.GlobalNamespace;
                var scriptClass = this.ScriptClass;

                if (mainTypeName != null)
                {
                    // Global code is the entry point, ignore all other Mains.
                    if (scriptClass is object)
                    {
                        // CONSIDER: we could use the symbol instead of just the name.
                        diagnostics.Add(ErrorCode.WRN_MainIgnored, NoLocation.Singleton, mainTypeName);
                        return scriptClass.GetScriptEntryPoint();
                    }

                    var nameParts = mainTypeName.Split('.');
                    if (nameParts.Any(n => string.IsNullOrWhiteSpace(n)))
                    {
                        diagnostics.Add(ErrorCode.ERR_BadCompilationOptionValue, NoLocation.Singleton, nameof(CSharpCompilationOptions.MainTypeName), mainTypeName);
                        return null;
                    }

                    var mainTypeOrNamespace = globalNamespace.GetNamespaceOrTypeByQualifiedName(nameParts).OfMinimalArity();
                    if (mainTypeOrNamespace is null)
                    {
                        diagnostics.Add(ErrorCode.ERR_MainClassNotFound, NoLocation.Singleton, mainTypeName);
                        return null;
                    }

                    mainType = mainTypeOrNamespace as NamedTypeSymbol;
                    if (mainType is null || mainType.IsGenericType || (mainType.TypeKind != TypeKind.Class && mainType.TypeKind != TypeKind.Struct && !mainType.IsInterface))
                    {
                        diagnostics.Add(ErrorCode.ERR_MainClassNotClass, mainTypeOrNamespace.GetFirstLocation(), mainTypeOrNamespace);
                        return null;
                    }

                    AddEntryPointCandidates(entryPointCandidates, mainType.GetMembersUnordered());
                }
                else
                {
                    mainType = null;

                    AddEntryPointCandidates(
                        entryPointCandidates,
                        this.GetSymbolsWithNameCore(WellKnownMemberNames.EntryPointMethodName, SymbolFilter.Member, cancellationToken));

                    // Global code is the entry point, ignore all other Mains.
                    if (scriptClass is object || simpleProgramEntryPointSymbol is object)
                    {
                        foreach (var main in entryPointCandidates)
                        {
                            if (main is not SynthesizedSimpleProgramEntryPointSymbol)
                            {
                                diagnostics.Add(ErrorCode.WRN_MainIgnored, main.GetFirstLocation(), main);
                            }
                        }

                        if (scriptClass is object)
                        {
                            return scriptClass.GetScriptEntryPoint();
                        }

                        RoslynDebug.Assert(simpleProgramEntryPointSymbol is object);
                        entryPointCandidates.Clear();
                        entryPointCandidates.Add(simpleProgramEntryPointSymbol);
                    }
                }

                // Validity and diagnostics are also tracked because they must be conditionally handled
                // if there are not any "traditional" entrypoints found.
                var taskEntryPoints = ArrayBuilder<(bool IsValid, MethodSymbol Candidate, BindingDiagnosticBag SpecificDiagnostics)>.GetInstance();

                // These diagnostics (warning only) are added to the compilation only if
                // there were not any main methods found.
                var noMainFoundDiagnostics = BindingDiagnosticBag.GetInstance(diagnostics);
                RoslynDebug.Assert(noMainFoundDiagnostics.DiagnosticBag is object);

                bool checkValid(MethodSymbol candidate, bool isCandidate, BindingDiagnosticBag specificDiagnostics)
                {
                    if (!isCandidate)
                    {
                        noMainFoundDiagnostics.Add(ErrorCode.WRN_InvalidMainSig, candidate.GetFirstLocation(), candidate);
                        noMainFoundDiagnostics.AddRange(specificDiagnostics);
                        return false;
                    }

                    if (candidate.IsGenericMethod || candidate.ContainingType.IsGenericType)
                    {
                        // a single error for partial methods:
                        noMainFoundDiagnostics.Add(ErrorCode.WRN_MainCantBeGeneric, candidate.GetFirstLocation(), candidate);
                        return false;
                    }
                    return true;
                }

                var viableEntryPoints = ArrayBuilder<MethodSymbol>.GetInstance();

                foreach (var candidate in entryPointCandidates)
                {
                    var perCandidateBag = BindingDiagnosticBag.GetInstance(diagnostics);
                    var (IsCandidate, IsTaskLike) = HasEntryPointSignature(candidate, perCandidateBag);

                    if (IsTaskLike)
                    {
                        taskEntryPoints.Add((IsCandidate, candidate, perCandidateBag));
                    }
                    else
                    {
                        if (checkValid(candidate, IsCandidate, perCandidateBag))
                        {
                            if (candidate.IsAsync)
                            {
                                diagnostics.Add(ErrorCode.ERR_NonTaskMainCantBeAsync, candidate.GetFirstLocation());
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
                        if (checkValid(Candidate, IsValid, SpecificDiagnostics) &&
                            CheckFeatureAvailability(Candidate.ExtractReturnTypeSyntax(), MessageID.IDS_FeatureAsyncMain, diagnostics))
                        {
                            diagnostics.AddRange(SpecificDiagnostics);
                            viableEntryPoints.Add(Candidate);
                        }
                    }
                }
                else if (LanguageVersion >= MessageID.IDS_FeatureAsyncMain.RequiredVersion() && taskEntryPoints.Count > 0)
                {
                    var taskCandidates = taskEntryPoints.SelectAsArray(s => (Symbol)s.Candidate);
                    var taskLocations = taskCandidates.SelectAsArray(s => s.GetFirstLocation());

                    foreach (var candidate in taskCandidates)
                    {
                        // Method '{0}' will not be used as an entry point because a synchronous entry point '{1}' was found.
                        var info = new CSDiagnosticInfo(
                             ErrorCode.WRN_SyncAndAsyncEntryPoints,
                             args: new object[] { candidate, viableEntryPoints[0] },
                             symbols: taskCandidates,
                             additionalLocations: taskLocations);
                        diagnostics.Add(new CSDiagnostic(info, candidate.GetFirstLocation()));
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
                else if (mainType is null)
                {
                    // Filters out diagnostics so that only InvalidMainSig and MainCant'BeGeneric are left.
                    // The reason that Error diagnostics can end up in `noMainFoundDiagnostics` is when
                    // HasEntryPointSignature yields some Error Diagnostics when people implement Task or Task<T> incorrectly.
                    //
                    // We can't add those Errors to the general diagnostics bag because it would break previously-working programs.
                    // The fact that these warnings are not added when csc is invoked with /main is possibly a bug, and is tracked at
                    // https://github.com/dotnet/roslyn/issues/18964
                    foreach (var diagnostic in noMainFoundDiagnostics.DiagnosticBag.AsEnumerable())
                    {
                        if (diagnostic.Code == (int)ErrorCode.WRN_InvalidMainSig || diagnostic.Code == (int)ErrorCode.WRN_MainCantBeGeneric)
                        {
                            diagnostics.Add(diagnostic);
                        }
                    }

                    diagnostics.AddDependencies(noMainFoundDiagnostics);
                }

                MethodSymbol? entryPoint = null;
                if (viableEntryPoints.Count == 0)
                {
                    if (mainType is null)
                    {
                        diagnostics.Add(ErrorCode.ERR_NoEntryPoint, NoLocation.Singleton);
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_NoMainInClass, mainType.GetFirstLocation(), mainType);
                    }
                }
                else
                {
                    foreach (var viableEntryPoint in viableEntryPoints)
                    {
                        if (viableEntryPoint.GetUnmanagedCallersOnlyAttributeData(forceComplete: true) is { } data)
                        {
                            Debug.Assert(!ReferenceEquals(data, UnmanagedCallersOnlyAttributeData.Uninitialized));
                            Debug.Assert(!ReferenceEquals(data, UnmanagedCallersOnlyAttributeData.AttributePresentDataNotBound));
                            diagnostics.Add(ErrorCode.ERR_EntryPointCannotBeUnmanagedCallersOnly, viableEntryPoint.GetFirstLocation());
                        }
                    }

                    if (viableEntryPoints.Count > 1)
                    {
                        viableEntryPoints.Sort(LexicalOrderSymbolComparer.Instance);
                        var info = new CSDiagnosticInfo(
                             ErrorCode.ERR_MultipleEntryPoints,
                             args: Array.Empty<object>(),
                             symbols: viableEntryPoints.OfType<Symbol>().AsImmutable(),
                             additionalLocations: viableEntryPoints.Select(m => m.GetFirstLocation()).OfType<Location>().AsImmutable());

                        diagnostics.Add(new CSDiagnostic(info, viableEntryPoints.First().GetFirstLocation()));
                    }
                    else
                    {
                        entryPoint = viableEntryPoints[0];
                    }
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
            ArrayBuilder<MethodSymbol> entryPointCandidates, IEnumerable<Symbol> members)
        {
            foreach (var member in members)
            {
                if (member.IsExtensionBlockMember())
                {
                    // When candidates are collected by GetSymbolsWithName, skeleton members are found but not implementation methods.
                    // We want to include the implementation for skeleton methods.
                    if (member is MethodSymbol method && method.TryGetCorrespondingExtensionImplementationMethod() is { } implementationMethod)
                    {
                        addIfCandidate(entryPointCandidates, implementationMethod);
                    }
                }
                else
                {
                    addIfCandidate(entryPointCandidates, member);
                }
            }

            static void addIfCandidate(ArrayBuilder<MethodSymbol> entryPointCandidates, Symbol member)
            {
                if (member is MethodSymbol method &&
                    method.IsEntryPointCandidate)
                {
                    entryPointCandidates.Add(method);
                }
            }
        }

        internal bool ReturnsAwaitableToVoidOrInt(MethodSymbol method, BindingDiagnosticBag diagnostics)
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
            var success = binder.GetAwaitableExpressionInfo(dumbInstance, out BoundExpression? result, out BoundCall? runtimeAwaitCall, syntax, diagnostics);

            RoslynDebug.Assert(!namedType.IsDynamic());
            if (!success)
            {
                return false;
            }

            Debug.Assert(result is { Type: not null } || runtimeAwaitCall is { Type: not null });
            var returnType = result?.Type ?? runtimeAwaitCall!.Type;
            return returnType.IsVoidType() || returnType.SpecialType == SpecialType.System_Int32;
        }

        /// <summary>
        /// Checks if the method has an entry point compatible signature, i.e.
        /// - the return type is either void, int, or returns a <see cref="System.Threading.Tasks.Task" />,
        /// or <see cref="System.Threading.Tasks.Task{T}" /> where the return type of GetAwaiter().GetResult()
        /// is either void or int.
        /// - has either no parameter or a single parameter of type string[]
        /// </summary>
        internal (bool IsCandidate, bool IsTaskLike) HasEntryPointSignature(MethodSymbol method, BindingDiagnosticBag bag)
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
            public readonly MethodSymbol? MethodSymbol;
            public readonly ReadOnlyBindingDiagnostic<AssemblySymbol> Diagnostics;

            public static readonly EntryPoint None = new EntryPoint(null, ReadOnlyBindingDiagnostic<AssemblySymbol>.Empty);

            public EntryPoint(MethodSymbol? methodSymbol, ReadOnlyBindingDiagnostic<AssemblySymbol> diagnostics)
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
            // https://github.com/dotnet/roslyn/issues/60397 : Add an API with ability to specify isChecked?

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

            TypeSymbol? cssource = source.EnsureCSharpSymbolOrNull(nameof(source));
            TypeSymbol? csdest = destination.EnsureCSharpSymbolOrNull(nameof(destination));

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;

            return Conversions.ClassifyConversionFromType(cssource, csdest, isChecked: false, ref discardedUseSiteInfo);
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
            // https://github.com/dotnet/roslyn/issues/60397 : Add an API with ability to specify isChecked?
            return ClassifyConversion(source, destination).ToCommonConversion();
        }

        internal override IConvertibleConversion ClassifyConvertibleConversion(IOperation source, ITypeSymbol? destination, out ConstantValue? constantValue)
        {
            constantValue = null;

            if (destination is null)
            {
                return Conversion.NoConversion;
            }

            ITypeSymbol? sourceType = source.Type;

            ConstantValue? sourceConstantValue = source.GetConstantValue();
            if (sourceType is null)
            {
                if (sourceConstantValue is { IsNull: true } && destination.IsReferenceType)
                {
                    constantValue = sourceConstantValue;
                    return Conversion.NullLiteral;
                }

                return Conversion.NoConversion;
            }

            Conversion result = ClassifyConversion(sourceType, destination);

            if (result.IsReference && sourceConstantValue is { IsNull: true })
            {
                constantValue = sourceConstantValue;
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
        internal PointerTypeSymbol CreatePointerTypeSymbol(TypeSymbol elementType, NullableAnnotation elementNullableAnnotation = NullableAnnotation.Oblivious)
        {
            if ((object)elementType == null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            return new PointerTypeSymbol(TypeWithAnnotations.Create(elementType, elementNullableAnnotation));
        }

        private protected override bool IsSymbolAccessibleWithinCore(
            ISymbol symbol,
            ISymbol within,
            ITypeSymbol? throughType)
        {
            Symbol? symbol0 = symbol.EnsureCSharpSymbolOrNull(nameof(symbol));
            Symbol? within0 = within.EnsureCSharpSymbolOrNull(nameof(within));
            TypeSymbol? throughType0 = throughType.EnsureCSharpSymbolOrNull(nameof(throughType));
            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            return
                within0.Kind == SymbolKind.Assembly ?
                AccessCheck.IsSymbolAccessible(symbol0, (AssemblySymbol)within0, ref discardedUseSiteInfo) :
                AccessCheck.IsSymbolAccessible(symbol0, (NamedTypeSymbol)within0, ref discardedUseSiteInfo, throughType0);
        }

        [Obsolete("Compilation.IsSymbolAccessibleWithin is not designed for use within the compilers", true)]
        internal new bool IsSymbolAccessibleWithin(
            ISymbol symbol,
            ISymbol within,
            ITypeSymbol? throughType = null)
        {
            throw new NotImplementedException();
        }

        private ConcurrentSet<MethodSymbol>? _moduleInitializerMethods;

        internal void AddModuleInitializerMethod(MethodSymbol method)
        {
            Debug.Assert(!_declarationDiagnosticsFrozen);
            LazyInitializer.EnsureInitialized(ref _moduleInitializerMethods).Add(method);
        }

        internal bool InterceptorsDiscoveryComplete;

        /// <remarks>Equals and GetHashCode on this type intentionally resemble corresponding methods on <see cref="InterceptableLocation1"/>.</remarks>
        private sealed class InterceptorKeyComparer : IEqualityComparer<(ImmutableArray<byte> ContentHash, int Position)>
        {
            private InterceptorKeyComparer() { }
            public static readonly InterceptorKeyComparer Instance = new InterceptorKeyComparer();

            public bool Equals((ImmutableArray<byte> ContentHash, int Position) x, (ImmutableArray<byte> ContentHash, int Position) y)
            {
                return x.ContentHash.SequenceEqual(y.ContentHash) && x.Position == y.Position;
            }

            public int GetHashCode((ImmutableArray<byte> ContentHash, int Position) obj)
            {
                return Hash.Combine(
                    BinaryPrimitives.ReadInt32LittleEndian(obj.ContentHash.AsSpan()),
                    obj.Position);
            }
        }

        // NB: the 'Many' case for these dictionary values means there are duplicates. An error is reported for this after binding.
        private ConcurrentDictionary<(ImmutableArray<byte> ContentHash, int Position), OneOrMany<(Location AttributeLocation, MethodSymbol Interceptor)>>? _interceptions;

        internal void AddInterception(ImmutableArray<byte> contentHash, int position, Location attributeLocation, MethodSymbol interceptor)
        {
            Debug.Assert(!_declarationDiagnosticsFrozen);
            Debug.Assert(!InterceptorsDiscoveryComplete);

            var dictionary = LazyInitializer.EnsureInitialized(ref _interceptions,
                () => new ConcurrentDictionary<(ImmutableArray<byte> ContentHash, int Position), OneOrMany<(Location AttributeLocation, MethodSymbol Interceptor)>>(comparer: InterceptorKeyComparer.Instance));
            dictionary.AddOrUpdate((contentHash, position),
                addValueFactory: static (key, newValue) => OneOrMany.Create(newValue),
                updateValueFactory: static (key, existingValues, newValue) =>
                {
                    // AddInterception can be called when attributes are decoded on a symbol, which can happen for the same symbol concurrently.
                    // If something else has already added the interceptor denoted by a given `[InterceptsLocation]`, we want to drop it.
                    // Since the collection is almost always length 1, a simple foreach is adequate for detecting this.
                    foreach (var (attributeLocation, interceptor) in existingValues)
                    {
                        if (attributeLocation == newValue.AttributeLocation && interceptor.Equals(newValue.Interceptor, TypeCompareKind.ConsiderEverything))
                        {
                            return existingValues;
                        }
                    }
                    return existingValues.Add(newValue);
                },
                // Explicit tuple element names are needed here so that the names unify when this is an extension method call (netstandard2.0).
                factoryArgument: (AttributeLocation: attributeLocation, Interceptor: interceptor));
        }

        internal (Location AttributeLocation, MethodSymbol Interceptor)? TryGetInterceptor(SimpleNameSyntax? node)
        {
            if (node is null)
            {
                return null;
            }

            ((SourceModuleSymbol)SourceModule).DiscoverInterceptorsIfNeeded();
            if (_interceptions is null)
            {
                return null;
            }

            var key = (node.SyntaxTree.GetText().GetContentHash(), node.Position);
            if (_interceptions.TryGetValue(key, out var interceptionsAtAGivenLocation) && interceptionsAtAGivenLocation is [var oneInterception])
            {
                return oneInterception;
            }

            return null;
        }

        #endregion

        #region Binding

        public new SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility)
#pragma warning disable RSEXPERIMENTAL001 // Internal usage of experimental API
            => GetSemanticModel(syntaxTree, ignoreAccessibility ? SemanticModelOptions.IgnoreAccessibility : SemanticModelOptions.None);
#pragma warning restore RSEXPERIMENTAL001

        /// <summary>
        /// Gets a new SyntaxTreeSemanticModel for the specified syntax tree.
        /// </summary>
        [Experimental(RoslynExperiments.NullableDisabledSemanticModel, UrlFormat = RoslynExperiments.NullableDisabledSemanticModel_Url)]
        public new SemanticModel GetSemanticModel(SyntaxTree syntaxTree, SemanticModelOptions options)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            if (!_syntaxAndDeclarations.GetLazyState().RootNamespaces.ContainsKey(syntaxTree))
            {
                throw new ArgumentException(CSharpResources.SyntaxTreeNotFound, nameof(syntaxTree));
            }

            SemanticModel? model = null;
            if (SemanticModelProvider != null)
            {
                model = SemanticModelProvider.GetSemanticModel(syntaxTree, this, options);
                Debug.Assert(model != null);
            }

            return model ?? CreateSemanticModel(syntaxTree, options);
        }

#pragma warning disable RSEXPERIMENTAL001 // Internal usage of experimental API
        internal override SemanticModel CreateSemanticModel(SyntaxTree syntaxTree, SemanticModelOptions options)
            => new SyntaxTreeSemanticModel(this, syntaxTree, options);
#pragma warning restore RSEXPERIMENTAL001

        // When building symbols from the declaration table (lazily), or inside a type, or when
        // compiling a method body, we may not have a BinderContext in hand for the enclosing
        // scopes.  Therefore, we build them when needed (and cache them) using a ContextBuilder.
        // Since a ContextBuilder is only a cache, and the identity of the ContextBuilders and
        // BinderContexts have no semantic meaning, we can reuse them or rebuild them, whichever is
        // most convenient.  We store them using weak references so that GC pressure will cause them
        // to be recycled.
        private WeakReference<BinderFactory>[]? _binderFactories;
        private WeakReference<BinderFactory>[]? _ignoreAccessibilityBinderFactories;

        internal BinderFactory GetBinderFactory(SyntaxTree syntaxTree, bool ignoreAccessibility = false)
        {
            if (ignoreAccessibility && SynthesizedSimpleProgramEntryPointSymbol.GetSimpleProgramEntryPoint(this) is object)
            {
                return GetBinderFactory(syntaxTree, ignoreAccessibility: true, ref _ignoreAccessibilityBinderFactories);
            }

            return GetBinderFactory(syntaxTree, ignoreAccessibility: false, ref _binderFactories);
        }

        private BinderFactory GetBinderFactory(SyntaxTree syntaxTree, bool ignoreAccessibility, ref WeakReference<BinderFactory>[]? cachedBinderFactories)
        {
            Debug.Assert(System.Runtime.CompilerServices.Unsafe.AreSame(ref cachedBinderFactories, ref ignoreAccessibility ? ref _ignoreAccessibilityBinderFactories : ref _binderFactories));

            var treeNum = GetSyntaxTreeOrdinal(syntaxTree);
            WeakReference<BinderFactory>[]? binderFactories = cachedBinderFactories;
            if (binderFactories == null)
            {
                binderFactories = new WeakReference<BinderFactory>[this.SyntaxTrees.Length];
                binderFactories = Interlocked.CompareExchange(ref cachedBinderFactories, binderFactories, null) ?? binderFactories;
            }

            BinderFactory? previousFactory;
            var previousWeakReference = binderFactories[treeNum];
            if (previousWeakReference != null && previousWeakReference.TryGetTarget(out previousFactory))
            {
                return previousFactory;
            }

            return AddNewFactory(syntaxTree, ignoreAccessibility, ref binderFactories[treeNum]);
        }

        private BinderFactory AddNewFactory(SyntaxTree syntaxTree, bool ignoreAccessibility, [NotNull] ref WeakReference<BinderFactory>? slot)
        {
            var newFactory = new BinderFactory(this, syntaxTree, ignoreAccessibility);
            var newWeakReference = new WeakReference<BinderFactory>(newFactory);

            while (true)
            {
                BinderFactory? previousFactory;
                WeakReference<BinderFactory>? previousWeakReference = slot;
                if (previousWeakReference != null && previousWeakReference.TryGetTarget(out previousFactory))
                {
                    Debug.Assert(slot is object);
                    return previousFactory;
                }

                if (Interlocked.CompareExchange(ref slot!, newWeakReference, previousWeakReference) == previousWeakReference)
                {
                    return newFactory;
                }
            }
        }

        internal Binder GetBinder(CSharpSyntaxNode syntax)
        {
            return GetBinderFactory(syntax.SyntaxTree).GetBinder(syntax);
        }

        private AliasSymbol CreateGlobalNamespaceAlias()
        {
            return AliasSymbol.CreateGlobalNamespaceAlias(this.GlobalNamespace);
        }

        private void CompleteTree(SyntaxTree tree)
        {
            if (_lazyCompilationUnitCompletedTrees == null) Interlocked.CompareExchange(ref _lazyCompilationUnitCompletedTrees, new HashSet<SyntaxTree>(), null);
            lock (_lazyCompilationUnitCompletedTrees)
            {
                if (_lazyCompilationUnitCompletedTrees.Add(tree))
                {
                    // signal the end of the compilation unit
                    EventQueue?.TryEnqueue(new CompilationUnitCompletedEvent(this, tree));

                    if (_lazyCompilationUnitCompletedTrees.Count == this.SyntaxTrees.Length)
                    {
                        // if that was the last tree, signal the end of compilation
                        CompleteCompilationEventQueue_NoLock();
                    }
                }
            }
        }

        internal override void ReportUnusedImports(DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            Debug.Assert(diagnostics is { });
            var bag = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            Debug.Assert(bag.DiagnosticBag is { });
            ReportUnusedImports(filterTree: null, bag, cancellationToken);
            diagnostics.AddRange(bag.DiagnosticBag);
            bag.Free();
        }

        private void ReportUnusedImports(SyntaxTree? filterTree, BindingDiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            if (_lazyImportInfos != null && (filterTree is null || ReportUnusedImportsInTree(filterTree)))
            {
                PooledHashSet<NamespaceSymbol>? externAliasesToCheck = null;

                if (diagnostics.DependenciesBag is object)
                {
                    externAliasesToCheck = PooledHashSet<NamespaceSymbol>.GetInstance();
                }

                foreach (var pair in _lazyImportInfos)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ImportInfo info = pair.Key;
                    SyntaxTree infoTree = info.Tree;
                    if ((filterTree == null || filterTree == infoTree) && ReportUnusedImportsInTree(infoTree))
                    {
                        TextSpan infoSpan = info.Span;
                        if (!this.IsImportDirectiveUsed(infoTree, infoSpan.Start))
                        {
                            ErrorCode code = info.Kind == SyntaxKind.ExternAliasDirective
                                ? ErrorCode.HDN_UnusedExternAlias
                                : ErrorCode.HDN_UnusedUsingDirective;
                            diagnostics.Add(code, infoTree.GetLocation(infoSpan));
                        }
                        else if (diagnostics.DependenciesBag is object)
                        {
                            RoslynDebug.Assert(externAliasesToCheck is object);
                            ImmutableArray<AssemblySymbol> dependencies = pair.Value;

                            if (!dependencies.IsDefaultOrEmpty)
                            {
                                diagnostics.AddDependencies(dependencies);
                            }
                            else if (info.Kind == SyntaxKind.ExternAliasDirective)
                            {
                                // Record targets of used extern aliases
                                var node = info.Tree.GetRoot(cancellationToken).FindToken(info.Span.Start, findInsideTrivia: false).
                                               Parent!.FirstAncestorOrSelf<ExternAliasDirectiveSyntax>();

                                if (node is object && GetExternAliasTarget(node.Identifier.ValueText, out NamespaceSymbol target))
                                {
                                    externAliasesToCheck.Add(target);
                                }
                            }
                        }
                    }
                }

                if (externAliasesToCheck is object)
                {
                    RoslynDebug.Assert(diagnostics.DependenciesBag is object);

                    // We could do this check after we have built the transitive closure
                    // in GetCompleteSetOfUsedAssemblies.completeTheSetOfUsedAssemblies. However,
                    // the level of accuracy is probably not worth the complexity this would add.
                    var bindingDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: false, withDependencies: true);
                    RoslynDebug.Assert(bindingDiagnostics.DependenciesBag is object);

                    foreach (var aliasedNamespace in externAliasesToCheck)
                    {
                        bindingDiagnostics.Clear();
                        bindingDiagnostics.AddAssembliesUsedByNamespaceReference(aliasedNamespace);

                        // See if any of the references with the alias are registered as used. We can get in a situation when none of them are.
                        // For example, when the alias was used in a doc comment, but nothing was found within it. We would get only a warning
                        // in this case and no assembly marked as used.
                        if (_lazyUsedAssemblyReferences?.IsEmpty == false || diagnostics.DependenciesBag.Count != 0)
                        {
                            foreach (var assembly in bindingDiagnostics.DependenciesBag)
                            {
                                if (_lazyUsedAssemblyReferences?.Contains(assembly) == true ||
                                    diagnostics.DependenciesBag.Contains(assembly))
                                {
                                    bindingDiagnostics.DependenciesBag.Clear();
                                    break;
                                }
                            }
                        }

                        diagnostics.AddDependencies(bindingDiagnostics);
                    }

                    bindingDiagnostics.Free();
                    externAliasesToCheck.Free();
                }
            }

            CompleteTrees(filterTree);
        }

        internal override void CompleteTrees(SyntaxTree? filterTree)
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

            if (filterTree is null)
            {
                _usageOfUsingsRecordedInTrees = null;
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
            // Note: the suppression will be unnecessary once LazyInitializer is properly annotated
            LazyInitializer.EnsureInitialized(ref _lazyImportInfos)!.
                TryAdd(new ImportInfo(syntax.SyntaxTree, syntax.Kind(), syntax.Span), default);
        }

        internal void RecordImportDependencies(UsingDirectiveSyntax syntax, ImmutableArray<AssemblySymbol> dependencies)
        {
            RoslynDebug.Assert(_lazyImportInfos is object);
            _lazyImportInfos.TryUpdate(new ImportInfo(syntax.SyntaxTree, syntax.Kind(), syntax.Span), dependencies, default);
        }

        private readonly struct ImportInfo : IEquatable<ImportInfo>
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

            public override bool Equals(object? obj)
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

        private DiagnosticBag? _lazyDeclarationDiagnostics;
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
        public override ImmutableArray<Diagnostic> GetParseDiagnostics(CancellationToken cancellationToken = default)
        {
            return GetDiagnostics(CompilationStage.Parse, false, symbolFilter: null, cancellationToken);
        }

        /// <summary>
        /// Gets the diagnostics produced during symbol declaration headers.  There are no diagnostics for accessor or
        /// method bodies, for example.
        /// </summary>
        public override ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default)
        {
            return GetDiagnostics(CompilationStage.Declare, false, symbolFilter: null, cancellationToken);
        }

        /// <summary>
        /// Gets the diagnostics produced during the analysis of method bodies and field initializers.
        /// </summary>
        public override ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(CancellationToken cancellationToken = default)
        {
            return GetDiagnostics(CompilationStage.Compile, false, symbolFilter: null, cancellationToken);
        }

        /// <summary>
        /// Gets the all the diagnostics for the compilation, including syntax, declaration, and binding. Does not
        /// include any diagnostics that might be produced during emit.
        /// </summary>
        public override ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default)
        {
            return GetDiagnostics(DefaultDiagnosticsStage, true, symbolFilter: null, cancellationToken);
        }

        internal ImmutableArray<Diagnostic> GetDiagnostics(CompilationStage stage, bool includeEarlierStages, Predicate<ISymbolInternal>? symbolFilter, CancellationToken cancellationToken)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            GetDiagnostics(stage, includeEarlierStages, diagnostics, symbolFilter, cancellationToken);
            return diagnostics.ToReadOnlyAndFree();
        }

        internal override void GetDiagnostics(CompilationStage stage, bool includeEarlierStages, DiagnosticBag diagnostics, CancellationToken cancellationToken = default)
            => GetDiagnostics(stage, includeEarlierStages, diagnostics, symbolFilter: null, cancellationToken);

        internal void GetDiagnostics(CompilationStage stage, bool includeEarlierStages, DiagnosticBag diagnostics, Predicate<ISymbolInternal>? symbolFilter, CancellationToken cancellationToken)
        {
            var builder = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            Debug.Assert(builder.DiagnosticBag is { });

            GetDiagnosticsWithoutSeverityFiltering(stage, includeEarlierStages, builder, symbolFilter, cancellationToken);

            // Before returning diagnostics, we filter warnings
            // to honor the compiler options (e.g., /nowarn, /warnaserror and /warn) and the pragmas.
            FilterAndAppendDiagnostics(diagnostics, builder.DiagnosticBag, cancellationToken);
            builder.Free();
        }

        private void GetDiagnosticsWithoutSeverityFiltering(CompilationStage stage, bool includeEarlierStages, BindingDiagnosticBag builder, Predicate<Symbol>? symbolFilter, CancellationToken cancellationToken)
        {
            RoslynDebug.Assert(builder.DiagnosticBag is object);

            if (stage == CompilationStage.Parse || (stage > CompilationStage.Parse && includeEarlierStages))
            {
                var syntaxTrees = this.SyntaxTrees;
                if (this.Options.ConcurrentBuild)
                {
                    RoslynParallel.For(
                        0,
                        syntaxTrees.Length,
                        UICultureUtilities.WithCurrentUICulture<int>(i =>
                        {
                            var syntaxTree = syntaxTrees[i];
                            AppendLoadDirectiveDiagnostics(builder.DiagnosticBag, _syntaxAndDeclarations, syntaxTree);
                            builder.AddRange(syntaxTree.GetDiagnostics(cancellationToken));
                        }),
                        cancellationToken);
                }
                else
                {
                    foreach (var syntaxTree in syntaxTrees)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        AppendLoadDirectiveDiagnostics(builder.DiagnosticBag, _syntaxAndDeclarations, syntaxTree);

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
                CheckAssemblyName(builder.DiagnosticBag);
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

                builder.AddRange(GetSourceDeclarationDiagnostics(symbolFilter: symbolFilter, cancellationToken: cancellationToken), allowMismatchInDependencyAccumulation: true);

                if (EventQueue != null && SyntaxTrees.Length == 0)
                {
                    EnsureCompilationEventQueueCompleted();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (stage == CompilationStage.Compile || stage > CompilationStage.Compile && includeEarlierStages)
            {
                var methodBodyDiagnostics = builder.AccumulatesDependencies ? BindingDiagnosticBag.GetConcurrentInstance() : BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                RoslynDebug.Assert(methodBodyDiagnostics.DiagnosticBag is object);
                GetDiagnosticsForAllMethodBodies(methodBodyDiagnostics, doLowering: false, cancellationToken);
                builder.AddRangeAndFree(methodBodyDiagnostics);
            }
        }

        private static void AppendLoadDirectiveDiagnostics(DiagnosticBag builder, SyntaxAndDeclarationManager syntaxAndDeclarations, SyntaxTree syntaxTree, Func<IEnumerable<Diagnostic>, IEnumerable<Diagnostic>>? locationFilterOpt = null)
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
        private void GetDiagnosticsForAllMethodBodies(BindingDiagnosticBag diagnostics, bool doLowering, CancellationToken cancellationToken)
        {
            RoslynDebug.Assert(diagnostics.DiagnosticBag is object);
            MethodCompiler.CompileMethodBodies(
                compilation: this,
                moduleBeingBuiltOpt: doLowering ? (PEModuleBuilder?)CreateModuleBuilder(
                                                                       emitOptions: EmitOptions.Default,
                                                                       debugEntryPoint: null,
                                                                       manifestResources: null,
                                                                       sourceLinkStream: null,
                                                                       embeddedTexts: null,
                                                                       testData: null,
                                                                       diagnostics: diagnostics.DiagnosticBag,
                                                                       cancellationToken: cancellationToken)
                                                : null,
                emittingPdb: false,
                hasDeclarationErrors: false,
                emitMethodBodies: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: cancellationToken);

            DocumentationCommentCompiler.WriteDocumentationCommentXml(this, null, null, diagnostics, cancellationToken);
            this.ReportUnusedImports(filterTree: null, diagnostics, cancellationToken);
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
            var bindingDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            Debug.Assert(bindingDiagnostics.DiagnosticBag is { });

            // Report unused directives only if computing diagnostics for the entire tree.
            // Otherwise we cannot determine if a particular directive is used outside of the given sub-span within the tree.
            bool reportUnusedUsings = (!span.HasValue || span.Value == tree.GetRoot(cancellationToken).FullSpan) && ReportUnusedImportsInTree(tree);
            bool recordUsageOfUsingsInAllTrees = false;

            if (reportUnusedUsings && UsageOfUsingsRecordedInTrees is not null)
            {
                foreach (var singleDeclaration in ((SourceNamespaceSymbol)SourceModule.GlobalNamespace).MergedDeclaration.Declarations)
                {
                    if (singleDeclaration.SyntaxReference.SyntaxTree == tree)
                    {
                        if (singleDeclaration.HasGlobalUsings)
                        {
                            // Global Using directives can be used in any tree. Make sure we collect usage information from all of them.
                            recordUsageOfUsingsInAllTrees = true;
                        }

                        break;
                    }
                }
            }

            if (recordUsageOfUsingsInAllTrees && UsageOfUsingsRecordedInTrees?.IsEmpty == true)
            {
                Debug.Assert(reportUnusedUsings);

                // Simply compile the world
                compileMethodBodiesAndDocComments(filterTree: null, filterSpan: null, bindingDiagnostics, cancellationToken);
                _usageOfUsingsRecordedInTrees = null;
            }
            else
            {
                // Always compile the target tree
                compileMethodBodiesAndDocComments(filterTree: tree, filterSpan: span, bindingDiagnostics, cancellationToken);

                if (reportUnusedUsings)
                {
                    registeredUsageOfUsingsInTree(tree);
                }

                // Compile other trees if we need to, but discard diagnostics from them.
                if (recordUsageOfUsingsInAllTrees)
                {
                    Debug.Assert(reportUnusedUsings);

                    var discarded = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                    Debug.Assert(discarded.DiagnosticBag is object);

                    foreach (var otherTree in SyntaxTrees)
                    {
                        var trackingSet = UsageOfUsingsRecordedInTrees;

                        if (trackingSet is null)
                        {
                            break;
                        }

                        if (!trackingSet.Contains(otherTree))
                        {
                            compileMethodBodiesAndDocComments(filterTree: otherTree, filterSpan: null, discarded, cancellationToken);
                            registeredUsageOfUsingsInTree(otherTree);
                            discarded.DiagnosticBag.Clear();
                        }
                    }

                    discarded.Free();
                }
            }

            if (reportUnusedUsings)
            {
                ReportUnusedImports(tree, bindingDiagnostics, cancellationToken);
            }

            return bindingDiagnostics.ToReadOnlyAndFree().Diagnostics;

            void compileMethodBodiesAndDocComments(SyntaxTree? filterTree, TextSpan? filterSpan, BindingDiagnosticBag bindingDiagnostics, CancellationToken cancellationToken)
            {
                MethodCompiler.CompileMethodBodies(
                                compilation: this,
                                moduleBeingBuiltOpt: null,
                                emittingPdb: false,
                                hasDeclarationErrors: false,
                                emitMethodBodies: false,
                                diagnostics: bindingDiagnostics,
                                filterOpt: filterTree is object ? (Predicate<Symbol>?)(s => IsDefinedOrImplementedInSourceTree(s, filterTree, filterSpan)) : (Predicate<Symbol>?)null,
                                cancellationToken: cancellationToken);

                DocumentationCommentCompiler.WriteDocumentationCommentXml(this, null, null, bindingDiagnostics, cancellationToken, filterTree, filterSpan);
            }

            void registeredUsageOfUsingsInTree(SyntaxTree tree)
            {
                var current = UsageOfUsingsRecordedInTrees;

                while (true)
                {
                    if (current is null)
                    {
                        break;
                    }

                    var updated = current.Add(tree);

                    if ((object)updated == current)
                    {
                        break;
                    }

                    if (updated.Count == SyntaxTrees.Length)
                    {
                        _usageOfUsingsRecordedInTrees = null;
                        break;
                    }

                    var recent = Interlocked.CompareExchange(ref _usageOfUsingsRecordedInTrees, updated, current);

                    if (recent == (object)current)
                    {
                        break;
                    }

                    current = recent;
                }
            }
        }

        private ReadOnlyBindingDiagnostic<AssemblySymbol> GetSourceDeclarationDiagnostics(SyntaxTree? syntaxTree = null, TextSpan? filterSpanWithinTree = null, Func<IEnumerable<Diagnostic>, SyntaxTree, TextSpan?, IEnumerable<Diagnostic>>? locationFilterOpt = null, Predicate<Symbol>? symbolFilter = null, CancellationToken cancellationToken = default)
        {
            UsingsFromOptions.Complete(this, cancellationToken);

            SourceLocation? location = null;
            if (syntaxTree != null)
            {
                var root = syntaxTree.GetRoot(cancellationToken);
                location = filterSpanWithinTree.HasValue ?
                    new SourceLocation(syntaxTree, filterSpanWithinTree.Value) :
                    new SourceLocation(root);
            }

            Assembly.ForceComplete(location, symbolFilter, cancellationToken);

            if (syntaxTree is null && symbolFilter is null)
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
                RoslynDebug.Assert(syntaxTree != null);
                result = locationFilterOpt(result, syntaxTree, filterSpanWithinTree);
            }

            // Do not check CLSCompliance if we are doing ENC.
            if (symbolFilter == null)
            {

                // NOTE: Concatenate the CLS diagnostics *after* filtering by tree/span, because they're already filtered.
                ReadOnlyBindingDiagnostic<AssemblySymbol> clsDiagnostics = GetClsComplianceDiagnostics(syntaxTree, filterSpanWithinTree, cancellationToken);
                return new ReadOnlyBindingDiagnostic<AssemblySymbol>(result.AsImmutable().Concat(clsDiagnostics.Diagnostics), clsDiagnostics.Dependencies);
            }
            else
            {
                return new ReadOnlyBindingDiagnostic<AssemblySymbol>(result.AsImmutable(), ImmutableArray<AssemblySymbol>.Empty);
            }
        }

        private ReadOnlyBindingDiagnostic<AssemblySymbol> GetClsComplianceDiagnostics(SyntaxTree? syntaxTree, TextSpan? filterSpanWithinTree, CancellationToken cancellationToken)
        {
            if (syntaxTree != null)
            {
                var builder = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                ClsComplianceChecker.CheckCompliance(this, builder, cancellationToken, syntaxTree, filterSpanWithinTree);
                return builder.ToReadOnlyAndFree();
            }

            if (_lazyClsComplianceDiagnostics.IsDefault || _lazyClsComplianceDependencies.IsDefault)
            {
                var builder = BindingDiagnosticBag.GetInstance();
                ClsComplianceChecker.CheckCompliance(this, builder, cancellationToken);
                var result = builder.ToReadOnlyAndFree();
                ImmutableInterlocked.InterlockedInitialize(ref _lazyClsComplianceDependencies, result.Dependencies);
                ImmutableInterlocked.InterlockedInitialize(ref _lazyClsComplianceDiagnostics, result.Diagnostics);
            }

            Debug.Assert(!_lazyClsComplianceDependencies.IsDefault);
            Debug.Assert(!_lazyClsComplianceDiagnostics.IsDefault);
            return new ReadOnlyBindingDiagnostic<AssemblySymbol>(_lazyClsComplianceDiagnostics, _lazyClsComplianceDependencies);
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
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DiagnosticBag? builder = DiagnosticBag.GetInstance();
            if (stage == CompilationStage.Parse || (stage > CompilationStage.Parse && includeEarlierStages))
            {
                AppendLoadDirectiveDiagnostics(builder, _syntaxAndDeclarations, syntaxTree,
                    diagnostics => FilterDiagnosticsByLocation(diagnostics, syntaxTree, filterSpanWithinTree));

                var syntaxDiagnostics = syntaxTree.GetDiagnostics(cancellationToken);
                syntaxDiagnostics = FilterDiagnosticsByLocation(syntaxDiagnostics, syntaxTree, filterSpanWithinTree);
                builder.AddRange(syntaxDiagnostics);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (stage == CompilationStage.Declare || (stage > CompilationStage.Declare && includeEarlierStages))
            {
                var declarationDiagnostics = GetSourceDeclarationDiagnostics(syntaxTree, filterSpanWithinTree, FilterDiagnosticsByLocation, symbolFilter: null, cancellationToken);
                // re-enabling/fixing the below assert is tracked by https://github.com/dotnet/roslyn/issues/21020
                // Debug.Assert(declarationDiagnostics.All(d => d.HasIntersectingLocation(syntaxTree, filterSpanWithinTree)));
                builder.AddRange(declarationDiagnostics.Diagnostics);
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
            FilterAndAppendAndFreeDiagnostics(result, ref builder, cancellationToken);
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

        internal override CommonPEModuleBuilder? CreateModuleBuilder(
            EmitOptions emitOptions,
            IMethodSymbol? debugEntryPoint,
            Stream? sourceLinkStream,
            IEnumerable<EmbeddedText>? embeddedTexts,
            IEnumerable<ResourceDescription>? manifestResources,
            CompilationTestData? testData,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!IsSubmission || HasCodeToEmit() ||
                         (emitOptions == EmitOptions.Default && debugEntryPoint is null && sourceLinkStream is null && embeddedTexts is null && manifestResources is null && testData is null));

            string? runtimeMDVersion = GetRuntimeMetadataVersion(emitOptions, diagnostics);
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
                moduleBeingBuilt.SetDebugEntryPoint(debugEntryPoint.GetSymbol(), diagnostics);
            }

            moduleBeingBuilt.SourceLinkStreamOpt = sourceLinkStream;

            if (embeddedTexts != null)
            {
                moduleBeingBuilt.EmbeddedTexts = embeddedTexts;
            }

            // testData is only passed when running tests.
            if (testData != null)
            {
                moduleBeingBuilt.SetTestData(testData);
            }

            return moduleBeingBuilt;
        }

        internal override bool CompileMethods(
            CommonPEModuleBuilder moduleBuilder,
            bool emittingPdb,
            DiagnosticBag diagnostics,
            Predicate<ISymbolInternal>? filterOpt,
            CancellationToken cancellationToken)
        {
            var emitMetadataOnly = moduleBuilder.EmitOptions.EmitMetadataOnly;

            // The diagnostics should include syntax and declaration errors. We insert these before calling Emitter.Emit, so that the emitter
            // does not attempt to emit if there are declaration errors (but we do insert all errors from method body binding...)
            PooledHashSet<int>? excludeDiagnostics = null;
            if (emitMetadataOnly)
            {
                excludeDiagnostics = PooledHashSet<int>.GetInstance();
                excludeDiagnostics.Add((int)ErrorCode.ERR_ConcreteMissingBody);
            }
            bool hasDeclarationErrors = !FilterAndAppendDiagnostics(diagnostics, GetDiagnostics(CompilationStage.Declare, true, symbolFilter: filterOpt, cancellationToken), excludeDiagnostics, cancellationToken);
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
                    diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, NoLocation.Singleton, ((Cci.INamedEntity)moduleBeingBuilt).Name,
                        new LocalizableResourceString(nameof(CodeAnalysisResources.ModuleHasInvalidAttributes), CodeAnalysisResources.ResourceManager, typeof(CodeAnalysisResources)));

                    return false;
                }

                SynthesizedMetadataCompiler.ProcessSynthesizedMembers(this, moduleBeingBuilt, cancellationToken);

                if (moduleBeingBuilt.OutputKind.IsApplication())
                {
                    var entryPointDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                    var entryPoint = MethodCompiler.GetEntryPoint(
                        this,
                        moduleBeingBuilt,
                        hasDeclarationErrors: false,
                        emitMethodBodies: false,
                        entryPointDiagnostics,
                        cancellationToken);
                    diagnostics.AddRange(entryPointDiagnostics.DiagnosticBag!);
                    bool shouldSetEntryPoint = entryPoint != null && !entryPointDiagnostics.HasAnyErrors();
                    entryPointDiagnostics.Free();
                    if (shouldSetEntryPoint)
                    {
                        moduleBeingBuilt.SetPEEntryPoint(entryPoint, diagnostics);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                if ((emittingPdb || moduleBeingBuilt.EmitOptions.InstrumentationKinds.Contains(InstrumentationKind.TestCoverage)) &&
                    !CreateDebugDocuments(moduleBeingBuilt.DebugDocumentsBuilder, moduleBeingBuilt.EmbeddedTexts, diagnostics))
                {
                    return false;
                }

                // Perform initial bind of method bodies in spite of earlier errors. This is the same
                // behavior as when calling GetDiagnostics()

                // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
                var methodBodyDiagnosticBag = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
                Debug.Assert(methodBodyDiagnosticBag.DiagnosticBag is { });
                Debug.Assert(moduleBeingBuilt is object);

                MethodCompiler.CompileMethodBodies(
                    this,
                    moduleBeingBuilt,
                    emittingPdb,
                    hasDeclarationErrors,
                    emitMethodBodies: true,
                    diagnostics: methodBodyDiagnosticBag,
                    filterOpt: filterOpt,
                    cancellationToken: cancellationToken);

                // We don't generate the module initializer for ENC scenarios, as the assembly is already loaded so edits to the module initializer would have no impact.
                Debug.Assert(filterOpt == null || moduleBeingBuilt.IsEncDelta);
                if (!hasDeclarationErrors && !CommonCompiler.HasUnsuppressableErrors(methodBodyDiagnosticBag.DiagnosticBag) && filterOpt == null)
                {
                    GenerateModuleInitializer(moduleBeingBuilt, methodBodyDiagnosticBag.DiagnosticBag);
                }

                bool hasDuplicateFilePaths = CheckDuplicateFilePaths(diagnostics);

                bool hasMethodBodyError = !FilterAndAppendDiagnostics(diagnostics, methodBodyDiagnosticBag.DiagnosticBag, cancellationToken);

                methodBodyDiagnosticBag.Free();
                if (hasDeclarationErrors || hasMethodBodyError || hasDuplicateFilePaths)
                {
                    return false;
                }
            }

            return true;
        }

        private protected override SymbolMatcher CreatePreviousToCurrentSourceAssemblyMatcher(
            EmitBaseline previousGeneration,
            SynthesizedTypeMaps otherSynthesizedTypes,
            IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> otherSynthesizedMembers,
            IReadOnlyDictionary<ISymbolInternal, ImmutableArray<ISymbolInternal>> otherDeletedMembers)
        {
            return new CSharpSymbolMatcher(
                sourceAssembly: ((CSharpCompilation)previousGeneration.Compilation).SourceAssembly,
                SourceAssembly,
                otherSynthesizedTypes,
                otherSynthesizedMembers,
                otherDeletedMembers);
        }

        private class DuplicateFilePathsVisitor : CSharpSymbolVisitor
        {
            // note: the default HashSet<string> uses an ordinal comparison
            private readonly PooledHashSet<string> _duplicatePaths = PooledHashSet<string>.GetInstance();

            private readonly DiagnosticBag _diagnostics;

            private bool _hasDuplicateFilePaths;

            public DuplicateFilePathsVisitor(DiagnosticBag diagnostics)
            {
                _diagnostics = diagnostics;
            }

            public bool CheckDuplicateFilePathsAndFree(ImmutableArray<SyntaxTree> syntaxTrees, NamespaceSymbol globalNamespace)
            {
                var paths = PooledHashSet<string>.GetInstance();
                foreach (var tree in syntaxTrees)
                {
                    if (!paths.Add(tree.FilePath))
                    {
                        _duplicatePaths.Add(tree.FilePath);
                    }
                }
                paths.Free();

                if (_duplicatePaths.Any())
                {
                    VisitNamespace(globalNamespace);
                }
                _duplicatePaths.Free();
                return _hasDuplicateFilePaths;
            }

            public override void VisitNamespace(NamespaceSymbol symbol)
            {
                foreach (var childSymbol in symbol.GetMembers())
                {
                    switch (childSymbol)
                    {
                        case NamespaceSymbol @namespace:
                            VisitNamespace(@namespace);
                            break;
                        case NamedTypeSymbol namedType:
                            VisitNamedType(namedType);
                            break;
                    }
                }
            }

            public override void VisitNamedType(NamedTypeSymbol symbol)
            {
                Debug.Assert(symbol.ContainingSymbol.Kind == SymbolKind.Namespace); // avoid unnecessary traversal of nested types
                if (symbol.IsFileLocal)
                {
                    var location = symbol.GetFirstLocation();
                    var filePath = location.SourceTree?.FilePath;
                    if (_duplicatePaths.Contains(filePath!))
                    {
                        _diagnostics.Add(ErrorCode.ERR_FileTypeNonUniquePath, location, symbol, filePath);
                        _hasDuplicateFilePaths = true;
                    }
                }
            }
        }

        /// <returns><see langword="true"/> if file types are present in files with duplicate file paths. Otherwise, <see langword="false" />.</returns>
        private bool CheckDuplicateFilePaths(DiagnosticBag diagnostics)
        {
            var visitor = new DuplicateFilePathsVisitor(diagnostics);
            return visitor.CheckDuplicateFilePathsAndFree(SyntaxTrees, GlobalNamespace);
        }

        /// <returns><see langword="true"/> if duplicate interceptions are present in the compilation. Otherwise, <see langword="false" />.</returns>
        internal bool CheckDuplicateInterceptions(BindingDiagnosticBag diagnostics)
        {
            if (_interceptions is null)
            {
                return false;
            }

            bool anyDuplicates = false;
            foreach ((_, OneOrMany<(Location, MethodSymbol)> interceptionsOfAGivenLocation) in _interceptions)
            {
                Debug.Assert(interceptionsOfAGivenLocation.Count != 0);
                if (interceptionsOfAGivenLocation.Count == 1)
                {
                    continue;
                }

                anyDuplicates = true;
                foreach (var (attributeLocation, _) in interceptionsOfAGivenLocation)
                {
                    diagnostics.Add(ErrorCode.ERR_DuplicateInterceptor, attributeLocation);
                }
            }

            return anyDuplicates;
        }

        private void GenerateModuleInitializer(PEModuleBuilder moduleBeingBuilt, DiagnosticBag methodBodyDiagnosticBag)
        {
            Debug.Assert(_declarationDiagnosticsFrozen);

            if (_moduleInitializerMethods is object)
            {
                var ilBuilder = new ILBuilder(moduleBeingBuilt, new LocalSlotManager(slotAllocator: null), methodBodyDiagnosticBag, OptimizationLevel.Release, areLocalsZeroed: false);

                foreach (MethodSymbol method in _moduleInitializerMethods.OrderBy<MethodSymbol>(LexicalOrderSymbolComparer.Instance))
                {
                    ilBuilder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);

                    ilBuilder.EmitToken(
                        moduleBeingBuilt.Translate(method, methodBodyDiagnosticBag, needDeclaration: true),
                        CSharpSyntaxTree.Dummy.GetRoot());
                }

                ilBuilder.EmitRet(isVoid: true);
                ilBuilder.Realize();
                moduleBeingBuilt.RootModuleType.SetStaticConstructorBody(ilBuilder.RealizedIL);
            }
        }

        internal override bool GenerateResources(
            CommonPEModuleBuilder moduleBuilder,
            Stream? win32Resources,
            bool useRawWin32Resources,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            DiagnosticBag? resourceDiagnostics = DiagnosticBag.GetInstance();

            SetupWin32Resources(moduleBuilder, win32Resources, useRawWin32Resources, resourceDiagnostics);

            ReportManifestResourceDuplicates(
                moduleBuilder.ManifestResources,
                SourceAssembly.Modules.Skip(1).Select(m => m.Name),   //all modules except the first one
                AddedModulesResourceNames(resourceDiagnostics),
                resourceDiagnostics);

            return FilterAndAppendAndFreeDiagnostics(diagnostics, ref resourceDiagnostics, cancellationToken);
        }

        internal override bool GenerateDocumentationComments(
            Stream? xmlDocStream,
            string? outputNameOverride,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            var xmlDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);
            Debug.Assert(xmlDiagnostics.DiagnosticBag is { });

            string? assemblyName = FileNameUtilities.ChangeExtension(outputNameOverride, extension: null);
            DocumentationCommentCompiler.WriteDocumentationCommentXml(this, assemblyName, xmlDocStream, xmlDiagnostics, cancellationToken);

            bool result = FilterAndAppendDiagnostics(diagnostics, xmlDiagnostics.DiagnosticBag, cancellationToken);

            xmlDiagnostics.Free();
            return result;
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
            EmitDifferenceOptions options,
            CompilationTestData? testData,
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
                options,
                testData,
                cancellationToken);
        }

        internal string? GetRuntimeMetadataVersion(EmitOptions emitOptions, DiagnosticBag diagnostics)
        {
            string? runtimeMDVersion = GetRuntimeMetadataVersion(emitOptions);
            if (runtimeMDVersion != null)
            {
                return runtimeMDVersion;
            }

            DiagnosticBag? runtimeMDVersionDiagnostics = DiagnosticBag.GetInstance();
            runtimeMDVersionDiagnostics.Add(ErrorCode.WRN_NoRuntimeMetadataVersion, NoLocation.Singleton);
            if (!FilterAndAppendAndFreeDiagnostics(diagnostics, ref runtimeMDVersionDiagnostics, CancellationToken.None))
            {
                return null;
            }

            return string.Empty; //prevent emitter from crashing.
        }

        private string? GetRuntimeMetadataVersion(EmitOptions emitOptions)
        {
            var corAssembly = Assembly.CorLibrary as Symbols.Metadata.PE.PEAssemblySymbol;

            if (corAssembly is object)
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
                        MakeChecksumBytes(checksumText),
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

        protected override Compilation CommonWithAssemblyName(string? assemblyName)
        {
            return WithAssemblyName(assemblyName);
        }

        protected override IAssemblySymbol CommonAssembly
        {
            get { return this.Assembly.GetPublicSymbol(); }
        }

        protected override INamespaceSymbol CommonGlobalNamespace
        {
            get { return this.GlobalNamespace.GetPublicSymbol(); }
        }

        protected override CompilationOptions CommonOptions
        {
            get { return _options; }
        }

        [Experimental(RoslynExperiments.NullableDisabledSemanticModel)]
        protected override SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, SemanticModelOptions options)
        {
            return this.GetSemanticModel(syntaxTree, options);
        }

        protected internal override ImmutableArray<SyntaxTree> CommonSyntaxTrees
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

        protected override Compilation CommonReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree? newTree)
        {
            return this.ReplaceSyntaxTree(oldTree, newTree);
        }

        protected override Compilation CommonWithOptions(CompilationOptions options)
        {
            return this.WithOptions((CSharpCompilationOptions)options);
        }

        protected override Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo? info)
        {
            return this.WithScriptCompilationInfo((CSharpScriptCompilationInfo?)info);
        }

        protected override bool CommonContainsSyntaxTree(SyntaxTree? syntaxTree)
        {
            return this.ContainsSyntaxTree(syntaxTree);
        }

        protected override ISymbol? CommonGetAssemblyOrModuleSymbol(MetadataReference reference)
        {
            return this.GetAssemblyOrModuleSymbol(reference).GetPublicSymbol();
        }

        protected override Compilation CommonClone()
        {
            return this.Clone();
        }

        protected override IModuleSymbol CommonSourceModule
        {
            get { return this.SourceModule.GetPublicSymbol(); }
        }

        private protected override INamedTypeSymbolInternal CommonGetSpecialType(SpecialType specialType)
        {
            return this.GetSpecialType(specialType);
        }

        protected override INamespaceSymbol? CommonGetCompilationNamespace(INamespaceSymbol namespaceSymbol)
        {
            return this.GetCompilationNamespace(namespaceSymbol).GetPublicSymbol();
        }

        protected override INamedTypeSymbol? CommonGetTypeByMetadataName(string metadataName)
        {
            return this.GetTypeByMetadataName(metadataName).GetPublicSymbol();
        }

        protected override INamedTypeSymbol? CommonScriptClass
        {
            get { return this.ScriptClass.GetPublicSymbol(); }
        }

        protected override IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank, CodeAnalysis.NullableAnnotation elementNullableAnnotation)
        {
            return CreateArrayTypeSymbol(elementType.EnsureCSharpSymbolOrNull(nameof(elementType)), rank, elementNullableAnnotation.ToInternalAnnotation()).GetPublicSymbol();
        }

        protected override IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType)
        {
            return CreatePointerTypeSymbol(elementType.EnsureCSharpSymbolOrNull(nameof(elementType)), elementType.NullableAnnotation.ToInternalAnnotation()).GetPublicSymbol();
        }

        protected override IFunctionPointerTypeSymbol CommonCreateFunctionPointerTypeSymbol(
            ITypeSymbol returnType,
            RefKind returnRefKind,
            ImmutableArray<ITypeSymbol> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            SignatureCallingConvention callingConvention,
            ImmutableArray<INamedTypeSymbol> callingConventionTypes)
        {
            if (returnType is null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            if (parameterTypes.IsDefault)
            {
                throw new ArgumentNullException(nameof(parameterTypes));
            }

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (parameterTypes[i] is null)
                {
                    throw new ArgumentNullException($"{nameof(parameterTypes)}[{i}]");
                }
            }

            if (parameterRefKinds.IsDefault)
            {
                throw new ArgumentNullException(nameof(parameterRefKinds));
            }

            if (parameterRefKinds.Length != parameterTypes.Length)
            {
                // Given {0} parameter types and {1} parameter ref kinds. These must be the same.
                throw new ArgumentException(string.Format(CSharpResources.NotSameNumberParameterTypesAndRefKinds, parameterTypes.Length, parameterRefKinds.Length));
            }

            if (returnRefKind == RefKind.Out)
            {
                //'RefKind.Out' is not a valid ref kind for a return type.
                throw new ArgumentException(CSharpResources.OutIsNotValidForReturn);
            }

            if (callingConvention != SignatureCallingConvention.Unmanaged && !callingConventionTypes.IsDefaultOrEmpty)
            {
                throw new ArgumentException(string.Format(CSharpResources.CallingConventionTypesRequireUnmanaged, nameof(callingConventionTypes), nameof(callingConvention)));
            }

            if (!callingConvention.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(callingConvention));
            }

            var returnTypeWithAnnotations = TypeWithAnnotations.Create(returnType.EnsureCSharpSymbolOrNull(nameof(returnType)), returnType.NullableAnnotation.ToInternalAnnotation());
            var parameterTypesWithAnnotations = parameterTypes.SelectAsArray(
                type => TypeWithAnnotations.Create(type.EnsureCSharpSymbolOrNull(nameof(parameterTypes)), type.NullableAnnotation.ToInternalAnnotation()));
            var internalCallingConvention = callingConvention.FromSignatureConvention();
            var conventionModifiers = internalCallingConvention == CallingConvention.Unmanaged && !callingConventionTypes.IsDefaultOrEmpty
                ? callingConventionTypes.SelectAsArray((type, i, @this) => getCustomModifierForType(type, @this, i), this)
                : ImmutableArray<CustomModifier>.Empty;

            return FunctionPointerTypeSymbol.CreateFromParts(
                internalCallingConvention,
                conventionModifiers,
                returnTypeWithAnnotations,
                returnRefKind: returnRefKind,
                parameterTypes: parameterTypesWithAnnotations,
                parameterRefKinds: parameterRefKinds,
                compilation: this).GetPublicSymbol();

            static CustomModifier getCustomModifierForType(INamedTypeSymbol type, CSharpCompilation @this, int index)
            {
                if (type is null)
                {
                    throw new ArgumentNullException($"{nameof(callingConventionTypes)}[{index}]");
                }

                var internalType = type.EnsureCSharpSymbolOrNull($"{nameof(callingConventionTypes)}[{index}]");
                if (!FunctionPointerTypeSymbol.IsCallingConventionModifier(internalType) || @this.Assembly.CorLibrary != internalType.ContainingAssembly)
                {
                    throw new ArgumentException(string.Format(CSharpResources.CallingConventionTypeIsInvalid, type.ToDisplayString()));
                }

                return CSharpCustomModifier.CreateOptional(internalType);
            }
        }

        protected override INamedTypeSymbol CommonCreateNativeIntegerTypeSymbol(bool signed)
        {
            return CreateNativeIntegerTypeSymbol(signed).GetPublicSymbol();
        }

        internal new NamedTypeSymbol CreateNativeIntegerTypeSymbol(bool signed)
        {
            return GetSpecialType(signed ? SpecialType.System_IntPtr : SpecialType.System_UIntPtr).AsNativeInteger();
        }

        protected override INamedTypeSymbol CommonCreateTupleTypeSymbol(
            ImmutableArray<ITypeSymbol> elementTypes,
            ImmutableArray<string?> elementNames,
            ImmutableArray<Location?> elementLocations,
            ImmutableArray<CodeAnalysis.NullableAnnotation> elementNullableAnnotations)
        {
            var typesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(elementTypes.Length);
            for (int i = 0; i < elementTypes.Length; i++)
            {
                ITypeSymbol typeSymbol = elementTypes[i];
                var elementType = typeSymbol.EnsureCSharpSymbolOrNull($"{nameof(elementTypes)}[{i}]");
                var annotation = (elementNullableAnnotations.IsDefault ? typeSymbol.NullableAnnotation : elementNullableAnnotations[i]).ToInternalAnnotation();
                typesBuilder.Add(TypeWithAnnotations.Create(elementType, annotation));
            }

            return NamedTypeSymbol.CreateTuple(
                locationOpt: null, // no location for the type declaration
                elementTypesWithAnnotations: typesBuilder.ToImmutableAndFree(),
                elementLocations: elementLocations,
                elementNames: elementNames,
                compilation: this,
                shouldCheckConstraints: false,
                includeNullability: false,
                errorPositions: default).GetPublicSymbol();
        }

        protected override INamedTypeSymbol CommonCreateTupleTypeSymbol(
            INamedTypeSymbol underlyingType,
            ImmutableArray<string?> elementNames,
            ImmutableArray<Location?> elementLocations,
            ImmutableArray<CodeAnalysis.NullableAnnotation> elementNullableAnnotations)
        {
            NamedTypeSymbol csharpUnderlyingTuple = underlyingType.EnsureCSharpSymbolOrNull(nameof(underlyingType));

            if (!csharpUnderlyingTuple.IsTupleTypeOfCardinality(out int cardinality))
            {
                throw new ArgumentException(CodeAnalysisResources.TupleUnderlyingTypeMustBeTupleCompatible, nameof(underlyingType));
            }

            elementNames = CheckTupleElementNames(cardinality, elementNames);
            CheckTupleElementLocations(cardinality, elementLocations);
            CheckTupleElementNullableAnnotations(cardinality, elementNullableAnnotations);

            var tupleType = NamedTypeSymbol.CreateTuple(
                csharpUnderlyingTuple, elementNames, elementLocations: elementLocations!);
            if (!elementNullableAnnotations.IsDefault)
            {
                tupleType = tupleType.WithElementTypes(
                    tupleType.TupleElementTypesWithAnnotations.ZipAsArray(
                        elementNullableAnnotations,
                        (t, a) => TypeWithAnnotations.Create(t.Type, a.ToInternalAnnotation())));
            }
            return tupleType.GetPublicSymbol();
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
                memberTypes[i].EnsureCSharpSymbolOrNull($"{nameof(memberTypes)}[{i}]");
            }

            if (!memberIsReadOnly.IsDefault && memberIsReadOnly.Any(static v => !v))
            {
                throw new ArgumentException($"Non-ReadOnly members are not supported in C# anonymous types.");
            }

            var fields = ArrayBuilder<AnonymousTypeField>.GetInstance();

            for (int i = 0, n = memberTypes.Length; i < n; i++)
            {
                var type = memberTypes[i].GetSymbol();
                var name = memberNames[i];
                var location = memberLocations.IsDefault ? Location.None : memberLocations[i];
                var nullableAnnotation = memberNullableAnnotations.IsDefault ? NullableAnnotation.Oblivious : memberNullableAnnotations[i].ToInternalAnnotation();
                fields.Add(new AnonymousTypeField(name, location, TypeWithAnnotations.Create(type, nullableAnnotation), RefKind.None, ScopedKind.None));
            }

            var descriptor = new AnonymousTypeDescriptor(fields.ToImmutableAndFree(), Location.None);

            return this.AnonymousTypeManager.ConstructAnonymousTypeSymbol(descriptor, BindingDiagnosticBag.Discarded).GetPublicSymbol();
        }

        protected override IMethodSymbol CommonCreateBuiltinOperator(
            string name,
            ITypeSymbol returnType,
            ITypeSymbol leftType,
            ITypeSymbol rightType)
        {
            var csharpReturnType = returnType.EnsureCSharpSymbolOrNull(nameof(returnType));
            var csharpLeftType = leftType.EnsureCSharpSymbolOrNull(nameof(leftType));
            var csharpRightType = rightType.EnsureCSharpSymbolOrNull(nameof(rightType));

            // caller already checked all of these were not null.
            Debug.Assert(csharpReturnType is not null);
            Debug.Assert(csharpLeftType is not null);
            Debug.Assert(csharpRightType is not null);

            var syntaxKind = SyntaxFacts.GetOperatorKind(name);
            if (syntaxKind == SyntaxKind.None)
                throw new ArgumentException(string.Format(CodeAnalysisResources.BadBuiltInOps1, name), nameof(name));

            var binaryOperatorName = OperatorFacts.BinaryOperatorNameFromSyntaxKindIfAny(syntaxKind, SyntaxFacts.IsCheckedOperator(name));
            if (binaryOperatorName != name)
                throw new ArgumentException(string.Format(CodeAnalysisResources.BadBuiltInOps3, name), nameof(name));

            // Lang specific checks to ensure this is an acceptable operator.
            validateSignature();

            return new SynthesizedIntrinsicOperatorSymbol(csharpLeftType, name, csharpRightType, csharpReturnType).GetPublicSymbol();

            void validateSignature()
            {
                // Dynamic built-in operators allow virtually all operations with all types.  So we do no further checking here.
                if (csharpReturnType.TypeKind is TypeKind.Dynamic ||
                    csharpLeftType.TypeKind is TypeKind.Dynamic ||
                    csharpRightType.TypeKind is TypeKind.Dynamic)
                {
                    return;
                }

                // Use fast-path check to see if this types are ok.
                var binaryKind = Binder.SyntaxKindToBinaryOperatorKind(SyntaxFacts.GetBinaryExpression(syntaxKind));

                if (csharpReturnType.SpecialType != SpecialType.None &&
                    csharpLeftType.SpecialType != SpecialType.None &&
                    csharpRightType.SpecialType != SpecialType.None)
                {
                    var easyOutBinaryKind = OverloadResolution.BinopEasyOut.OpKind(binaryKind, csharpLeftType, csharpRightType);

                    if (easyOutBinaryKind != BinaryOperatorKind.Error)
                    {
                        var signature = this.BuiltInOperators.GetSignature(easyOutBinaryKind);
                        if (csharpReturnType.SpecialType == signature.ReturnType.SpecialType &&
                            csharpLeftType.SpecialType == signature.LeftType.SpecialType &&
                            csharpRightType.SpecialType == signature.RightType.SpecialType)
                        {
                            return;
                        }
                    }
                }

                // bool operator ==(object, object) is legal.
                // bool operator !=(object, object) is legal.
                // bool operator ==(Delegate, Delegate) is legal.
                // bool operator !=(Delegate, Delegate) is legal.
                if (binaryKind is BinaryOperatorKind.Equal or BinaryOperatorKind.NotEqual &&
                    csharpReturnType.SpecialType is SpecialType.System_Boolean)
                {
                    if ((csharpLeftType.SpecialType, csharpRightType.SpecialType) is
                            (SpecialType.System_Object, SpecialType.System_Object) or
                            (SpecialType.System_Delegate, SpecialType.System_Delegate))
                    {
                        return;
                    }
                }

                // Actual delegates have several operators that can be used on them.
                if (csharpLeftType.TypeKind is TypeKind.Delegate &&
                    TypeSymbol.Equals(csharpLeftType, csharpRightType, TypeCompareKind.ConsiderEverything))
                {
                    // bool operator ==(SomeDelegate, SomeDelegate) is legal.
                    // bool operator !=(SomeDelegate, SomeDelegate) is legal.
                    if (binaryKind is BinaryOperatorKind.Equal or BinaryOperatorKind.NotEqual &&
                        csharpReturnType.SpecialType == SpecialType.System_Boolean)
                    {
                        return;
                    }

                    // SomeDelegate operator +(SomeDelegate, SomeDelegate) is legal.
                    // SomeDelegate operator -(SomeDelegate, SomeDelegate) is legal.
                    if (binaryKind is BinaryOperatorKind.Addition or BinaryOperatorKind.Subtraction &&
                        TypeSymbol.Equals(csharpLeftType, csharpReturnType, TypeCompareKind.ConsiderEverything))
                    {
                        return;
                    }
                }

                if (csharpLeftType.IsEnumType() || csharpRightType.IsEnumType())
                {
                    // bool operator ==(SomeEnum, SomeEnum) is legal.
                    // bool operator !=(SomeEnum, SomeEnum) is legal.
                    // bool operator >(SomeEnum, SomeEnum) is legal.
                    // bool operator <(SomeEnum, SomeEnum) is legal.
                    // bool operator >=(SomeEnum, SomeEnum) is legal.
                    // bool operator <=(SomeEnum, SomeEnum) is legal.
                    if (binaryKind is BinaryOperatorKind.Equal or
                                      BinaryOperatorKind.NotEqual or
                                      BinaryOperatorKind.GreaterThan or
                                      BinaryOperatorKind.LessThan or
                                      BinaryOperatorKind.GreaterThanOrEqual or
                                      BinaryOperatorKind.LessThanOrEqual &&
                        csharpReturnType.SpecialType is SpecialType.System_Boolean &&
                        TypeSymbol.Equals(csharpLeftType, csharpRightType, TypeCompareKind.ConsiderEverything))
                    {
                        return;
                    }

                    // SomeEnum operator &(SomeEnum, SomeEnum) is legal.
                    // SomeEnum operator |(SomeEnum, SomeEnum) is legal.
                    // SomeEnum operator ^(SomeEnum, SomeEnum) is legal.
                    if (binaryKind is BinaryOperatorKind.And or
                                      BinaryOperatorKind.Or or
                                      BinaryOperatorKind.Xor &&
                        TypeSymbol.Equals(csharpLeftType, csharpRightType, TypeCompareKind.ConsiderEverything) &&
                        TypeSymbol.Equals(csharpReturnType, csharpRightType, TypeCompareKind.ConsiderEverything))
                    {
                        return;
                    }

                    // SomeEnum operator+(SomeEnum, EnumUnderlyingInt)
                    // SomeEnum operator+(EnumUnderlyingInt, SomeEnum)
                    // SomeEnum operator-(SomeEnum, EnumUnderlyingInt)
                    // SomeEnum operator-(EnumUnderlyingInt, SomeEnum)
                    if (binaryKind is BinaryOperatorKind.Addition or BinaryOperatorKind.Subtraction)
                    {
                        if (csharpLeftType.IsEnumType() &&
                            csharpRightType.SpecialType == csharpLeftType.GetEnumUnderlyingType()?.SpecialType &&
                            TypeSymbol.Equals(csharpLeftType, csharpReturnType, TypeCompareKind.ConsiderEverything))
                        {
                            return;
                        }

                        if (csharpRightType.IsEnumType() &&
                            csharpLeftType.SpecialType == csharpRightType.GetEnumUnderlyingType()?.SpecialType &&
                            TypeSymbol.Equals(csharpRightType, csharpReturnType, TypeCompareKind.ConsiderEverything))
                        {
                            return;
                        }
                    }

                    // EnumUnderlyingInt operator-(SomeEnum, SomeEnum)
                    if (binaryKind is BinaryOperatorKind.Subtraction &&
                        csharpReturnType.SpecialType == csharpLeftType.GetEnumUnderlyingType()?.SpecialType &&
                        TypeSymbol.Equals(csharpLeftType, csharpRightType, TypeCompareKind.ConsiderEverything))
                    {
                        return;
                    }
                }

                // void* has several comparison operators built in.
                if (binaryKind is BinaryOperatorKind.Equal or
                                  BinaryOperatorKind.NotEqual or
                                  BinaryOperatorKind.GreaterThan or
                                  BinaryOperatorKind.LessThan or
                                  BinaryOperatorKind.GreaterThanOrEqual or
                                  BinaryOperatorKind.LessThanOrEqual &&
                    csharpReturnType.SpecialType is SpecialType.System_Boolean &&
                    csharpLeftType is PointerTypeSymbol { PointedAtType.SpecialType: SpecialType.System_Void } &&
                    csharpRightType is PointerTypeSymbol { PointedAtType.SpecialType: SpecialType.System_Void })
                {
                    return;
                }

                // T* operator+(T*, int/uint/long/ulong i)
                if (binaryKind is BinaryOperatorKind.Addition &&
                    csharpLeftType.IsPointerType() &&
                    isAllowedPointerArithmeticIntegralType(csharpRightType) &&
                    TypeSymbol.Equals(csharpLeftType, csharpReturnType, TypeCompareKind.ConsiderEverything))
                {
                    return;
                }

                // T* operator+(int/uint/long/ulong i, T*)
                if (binaryKind is BinaryOperatorKind.Addition &&
                    csharpRightType.IsPointerType() &&
                    isAllowedPointerArithmeticIntegralType(csharpLeftType) &&
                    TypeSymbol.Equals(csharpRightType, csharpReturnType, TypeCompareKind.ConsiderEverything))
                {
                    return;
                }

                // T* operator-(T*, int/uint/long/ulong i)
                if (binaryKind is BinaryOperatorKind.Subtraction &&
                    csharpLeftType.IsPointerType() &&
                    isAllowedPointerArithmeticIntegralType(csharpRightType) &&
                    TypeSymbol.Equals(csharpLeftType, csharpReturnType, TypeCompareKind.ConsiderEverything))
                {
                    return;
                }

                // long operator-(T*, T*)
                if (binaryKind is BinaryOperatorKind.Subtraction &&
                    csharpLeftType.IsPointerType() &&
                    csharpReturnType.SpecialType is SpecialType.System_Int64 &&
                    TypeSymbol.Equals(csharpLeftType, csharpRightType, TypeCompareKind.ConsiderEverything))
                {
                    return;
                }

                // ROS<byte> operator+(ROS<byte>, ROS<byte>). Legal because of utf8 strings.
                if (binaryKind is BinaryOperatorKind.Addition &&
                    isReadOnlySpanOfByteType(csharpReturnType) &&
                    isReadOnlySpanOfByteType(csharpLeftType) &&
                    isReadOnlySpanOfByteType(csharpRightType))
                {
                    return;
                }

                throw new ArgumentException(string.Format(CodeAnalysisResources.BadBuiltInOps2, $"{csharpReturnType.ToDisplayString()} operator {name}({csharpLeftType.ToDisplayString()}, {csharpRightType.ToDisplayString()})"));
            }

            bool isAllowedPointerArithmeticIntegralType(TypeSymbol type)
                => type.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64;

            bool isReadOnlySpanOfByteType(TypeSymbol type)
                => IsReadOnlySpanType(type) && ((NamedTypeSymbol)type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].SpecialType == SpecialType.System_Byte;
        }

        protected override IMethodSymbol CommonCreateBuiltinOperator(
            string name,
            ITypeSymbol returnType,
            ITypeSymbol operandType)
        {
            var csharpReturnType = returnType.EnsureCSharpSymbolOrNull(nameof(returnType));
            var csharpOperandType = operandType.EnsureCSharpSymbolOrNull(nameof(operandType));

            // caller already checked all of these were not null.
            Debug.Assert(csharpReturnType is not null);
            Debug.Assert(csharpOperandType is not null);

            var syntaxKind = SyntaxFacts.GetOperatorKind(name);

            // Currently compiler does not generate built-ins for `operator true/false`.  If that changes, this check
            // can be relaxed.
            if (syntaxKind == SyntaxKind.None || name is WellKnownMemberNames.TrueOperatorName or WellKnownMemberNames.FalseOperatorName)
                throw new ArgumentException(string.Format(CodeAnalysisResources.BadBuiltInOps1, name), nameof(name));

            var unaryOperatorName = OperatorFacts.UnaryOperatorNameFromSyntaxKindIfAny(syntaxKind, SyntaxFacts.IsCheckedOperator(name));
            if (unaryOperatorName != name)
                throw new ArgumentException(string.Format(CodeAnalysisResources.BadBuiltInOps3, name), nameof(name));

            // Lang specific checks to ensure this is an acceptable operator.
            validateSignature();

            return new SynthesizedIntrinsicOperatorSymbol(csharpOperandType, name, csharpReturnType).GetPublicSymbol();

            void validateSignature()
            {
                // Dynamic built-in operators allow virtually all operations with all types.  So we do no further checking here.
                if (csharpReturnType.TypeKind is TypeKind.Dynamic ||
                    csharpOperandType.TypeKind is TypeKind.Dynamic)
                {
                    return;
                }

                var unaryKind = Binder.SyntaxKindToUnaryOperatorKind(SyntaxFacts.GetPrefixUnaryExpression(syntaxKind));

                // Use fast-path check to see if this types are ok.
                if (csharpReturnType.SpecialType != SpecialType.None && csharpOperandType.SpecialType != SpecialType.None)
                {
                    var easyOutUnaryKind = OverloadResolution.UnopEasyOut.OpKind(unaryKind, csharpOperandType);

                    if (easyOutUnaryKind != UnaryOperatorKind.Error)
                    {
                        var signature = this.BuiltInOperators.GetSignature(easyOutUnaryKind);
                        if (csharpReturnType.SpecialType == signature.ReturnType.SpecialType &&
                            csharpOperandType.SpecialType == signature.OperandType.SpecialType)
                        {
                            return;
                        }
                    }
                }

                // EnumType operator++(EnumType)
                // EnumType operator~(EnumType)
                if (csharpOperandType.IsEnumType() &&
                    unaryKind is UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PrefixDecrement or UnaryOperatorKind.BitwiseComplement &&
                    TypeSymbol.Equals(csharpOperandType, csharpReturnType, TypeCompareKind.ConsiderEverything))
                {
                    return;
                }

                // T* operator++(T*)
                if (csharpOperandType.IsPointerType() &&
                    unaryKind is UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PrefixDecrement &&
                    TypeSymbol.Equals(csharpOperandType, csharpReturnType, TypeCompareKind.ConsiderEverything))
                {
                    return;
                }

                throw new ArgumentException(string.Format(CodeAnalysisResources.BadBuiltInOps2, $"{csharpReturnType.ToDisplayString()} operator {name}({csharpOperandType.ToDisplayString()})"));
            }
        }

        protected override ITypeSymbol CommonDynamicType
        {
            get { return DynamicType.GetPublicSymbol(); }
        }

        protected override INamedTypeSymbol CommonObjectType
        {
            get { return this.ObjectType.GetPublicSymbol(); }
        }

        protected override IMethodSymbol? CommonGetEntryPoint(CancellationToken cancellationToken)
        {
            return this.GetEntryPoint(cancellationToken).GetPublicSymbol();
        }

        internal override int CompareSourceLocations(Location loc1, Location loc2)
        {
            Debug.Assert(loc1.IsInSource);
            Debug.Assert(loc2.IsInSource);

            var comparison = CompareSyntaxTreeOrdering(loc1.SourceTree!, loc2.SourceTree!);
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

        internal override int CompareSourceLocations(SyntaxNode loc1, SyntaxNode loc2)
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
        public override bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
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
        public override IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (filter == SymbolFilter.None)
            {
                throw new ArgumentException(CSharpResources.NoNoneSearchCriteria, nameof(filter));
            }

            return new PredicateSymbolSearcher(this, filter, predicate, cancellationToken).GetSymbolsWithName().GetPublicSymbols()!;
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Return true if there is a source declaration symbol name that matches the provided name.
        /// This will be faster than <see cref="ContainsSymbolsWithName(Func{string, bool}, SymbolFilter, CancellationToken)"/>
        /// when predicate is just a simple string check.
        /// </summary>
        public override bool ContainsSymbolsWithName(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
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
        public override IEnumerable<ISymbol> GetSymbolsWithName(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
        {
            return GetSymbolsWithNameCore(name, filter, cancellationToken).GetPublicSymbols()!;
        }

        internal IEnumerable<Symbol> GetSymbolsWithNameCore(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default)
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
        internal bool HasDynamicEmitAttributes(BindingDiagnosticBag diagnostics, Location location)
        {
            return Binder.GetWellKnownTypeMember(this, WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctor, diagnostics, location) is object &&
                   Binder.GetWellKnownTypeMember(this, WellKnownMember.System_Runtime_CompilerServices_DynamicAttribute__ctorTransformFlags, diagnostics, location) is object;
        }

        internal bool HasTupleNamesAttributes(BindingDiagnosticBag diagnostics, Location location) =>
            Binder.GetWellKnownTypeMember(this, WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames, diagnostics, location) is object;

        /// <summary>
        /// Returns whether the compilation has the Boolean type and if it's good.
        /// </summary>
        /// <returns>Returns true if Boolean is present and healthy.</returns>
        internal bool CanEmitBoolean() => CanEmitSpecialType(SpecialType.System_Boolean);

        internal bool CanEmitSpecialType(SpecialType type)
        {
            var typeSymbol = GetSpecialType(type);
            var diagnostic = typeSymbol.GetUseSiteInfo().DiagnosticInfo;
            return (diagnostic == null) || (diagnostic.Severity != DiagnosticSeverity.Error);
        }

        internal bool EmitNullablePublicOnly
        {
            get
            {
                if (!_lazyEmitNullablePublicOnly.HasValue())
                {
                    bool value = SyntaxTrees.FirstOrDefault()?.Options?.HasFeature(FeatureFlag.NullablePublicOnly) == true;
                    _lazyEmitNullablePublicOnly = value.ToThreeState();
                }
                return _lazyEmitNullablePublicOnly.Value();
            }
        }

        internal bool ShouldEmitNativeIntegerAttributes()
        {
            return !Assembly.RuntimeSupportsNumericIntPtr;
        }

        internal bool ShouldEmitNullableAttributes(Symbol symbol)
        {
            RoslynDebug.Assert(symbol is object);
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

        internal override AnalyzerDriver CreateAnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager, SeverityFilter severityFilter)
        {
            Func<SyntaxNode, SyntaxKind> getKind = node => node.Kind();
            Func<SyntaxTrivia, bool> isComment = trivia => trivia.Kind() == SyntaxKind.SingleLineCommentTrivia || trivia.Kind() == SyntaxKind.MultiLineCommentTrivia;
            return new AnalyzerDriver<SyntaxKind>(analyzers, getKind, analyzerManager, severityFilter, isComment);
        }

        internal void SymbolDeclaredEvent(Symbol symbol)
        {
            EventQueue?.TryEnqueue(new SymbolDeclaredCompilationEvent(this, symbol));
        }

        internal override void SerializePdbEmbeddedCompilationOptions(BlobBuilder builder)
        {
            // LanguageVersion should already be mapped to a specific version
            Debug.Assert(LanguageVersion == LanguageVersion.MapSpecifiedToEffectiveVersion());
            writeValue(CompilationOptionNames.LanguageVersion, LanguageVersion.ToDisplayString());

            if (Options.CheckOverflow)
            {
                writeValue(CompilationOptionNames.Checked, Options.CheckOverflow.ToString());
            }

            if (Options.NullableContextOptions != NullableContextOptions.Disable)
            {
                writeValue(CompilationOptionNames.Nullable, Options.NullableContextOptions.ToString());
            }

            if (Options.AllowUnsafe)
            {
                writeValue(CompilationOptionNames.Unsafe, Options.AllowUnsafe.ToString());
            }

            var preprocessorSymbols = GetPreprocessorSymbols();
            if (preprocessorSymbols.Any())
            {
                writeValue(CompilationOptionNames.Define, string.Join(",", preprocessorSymbols));
            }

            void writeValue(string key, string value)
            {
                builder.WriteUTF8(key);
                builder.WriteByte(0);
                builder.WriteUTF8(value);
                builder.WriteByte(0);
            }
        }

        private ImmutableArray<string> GetPreprocessorSymbols()
        {
            CSharpSyntaxTree? firstTree = (CSharpSyntaxTree?)SyntaxTrees.FirstOrDefault();

            if (firstTree is null)
            {
                return ImmutableArray<string>.Empty;
            }

            return firstTree.Options.PreprocessorSymbolNames.ToImmutableArray();
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

        private protected override bool SupportsRuntimeCapabilityCore(RuntimeCapability capability)
            => this.Assembly.SupportsRuntimeCapability(capability);

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

            public IEnumerable<Symbol> GetSymbolsWithName()
            {
                var result = new HashSet<Symbol>();
                var spine = ArrayBuilder<MergedNamespaceOrTypeDeclaration>.GetInstance();

                AppendSymbolsWithName(spine, _compilation.MergedRootDeclaration, result);

                spine.Free();
                _cache.Free();
                return result;
            }

            private void AppendSymbolsWithName(
                ArrayBuilder<MergedNamespaceOrTypeDeclaration> spine, MergedNamespaceOrTypeDeclaration current,
                HashSet<Symbol> set)
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
                ArrayBuilder<MergedNamespaceOrTypeDeclaration> spine, MergedTypeDeclaration current, HashSet<Symbol> set)
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

            protected NamespaceOrTypeSymbol? GetSpineSymbol(ArrayBuilder<MergedNamespaceOrTypeDeclaration> spine)
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

                NamespaceOrTypeSymbol? current = _compilation.GlobalNamespace;
                for (var i = 1; i < spine.Count; i++)
                {
                    current = GetSymbol(current, spine[i]);
                }

                return current;
            }

            private NamespaceOrTypeSymbol? GetCachedSymbol(MergedNamespaceOrTypeDeclaration declaration)
                => _cache.TryGetValue(declaration, out NamespaceOrTypeSymbol? symbol)
                        ? symbol
                        : null;

            private NamespaceOrTypeSymbol? GetSymbol(NamespaceOrTypeSymbol? container, MergedNamespaceOrTypeDeclaration declaration)
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
                    if (sourceType is object)
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
                    if (typeDecl.MemberNames.Value.Contains(_name))
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

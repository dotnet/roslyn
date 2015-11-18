// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The compilation object is an immutable representation of a single invocation of the
    /// compiler. Although immutable, a compilation is also on-demand, and will realize and cache
    /// data as necessary. A compilation can produce a new compilation from existing compilation
    /// with the application of small deltas. In many cases, it is more efficient than creating a
    /// new compilation from scratch, as the new compilation can reuse information from the old
    /// compilation.
    /// </summary>
    public abstract partial class Compilation
    {
        /// <summary>
        /// Returns true if this is a case sensitive compilation, false otherwise.  Case sensitivity
        /// affects compilation features such as name lookup as well as choosing what names to emit
        /// when there are multiple different choices (for example between a virtual method and an
        /// override).
        /// </summary>
        public abstract bool IsCaseSensitive { get; }

        /// <summary>
        /// Used for test purposes only to emulate missing members.
        /// </summary>
        private SmallDictionary<int, bool> _lazyMakeWellKnownTypeMissingMap;

        /// <summary>
        /// Used for test purposes only to emulate missing members.
        /// </summary>
        private SmallDictionary<int, bool> _lazyMakeMemberMissingMap;

        private readonly IReadOnlyDictionary<string, string> _features;

        public ScriptCompilationInfo ScriptCompilationInfo => CommonScriptCompilationInfo;
        internal abstract ScriptCompilationInfo CommonScriptCompilationInfo { get; }

        internal Compilation(
            string name,
            ImmutableArray<MetadataReference> references,
            IReadOnlyDictionary<string, string> features,
            bool isSubmission,
            AsyncQueue<CompilationEvent> eventQueue)
        {
            Debug.Assert(!references.IsDefault);
            Debug.Assert(features != null);

            this.AssemblyName = name;
            this.ExternalReferences = references;
            this.EventQueue = eventQueue;

            _lazySubmissionSlotIndex = isSubmission ? SubmissionSlotIndexToBeAllocated : SubmissionSlotIndexNotApplicable;
            _features = features;
        }

        protected static IReadOnlyDictionary<string, string> SyntaxTreeCommonFeatures(IEnumerable<SyntaxTree> trees)
        {
            IReadOnlyDictionary<string, string> set = null;

            foreach (var tree in trees)
            {
                var treeFeatures = tree.Options.Features;
                if (set == null)
                {
                    set = treeFeatures;
                }
                else
                {
                    if ((object)set != treeFeatures && !set.SetEquals(treeFeatures))
                    {
                        throw new ArgumentException("inconsistent syntax tree features", nameof(trees));
                    }
                }
            }

            if (set == null)
            {
                // Edge case where there are no syntax trees
                set = ImmutableDictionary<string, string>.Empty;
            }

            return set;
        }

        internal abstract AnalyzerDriver AnalyzerForLanguage(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager);

        /// <summary>
        /// Gets the source language ("C#" or "Visual Basic").
        /// </summary>
        public abstract string Language { get; }

        internal static void ValidateScriptCompilationParameters(Compilation previousScriptCompilation, Type returnType, ref Type globalsType)
        {
            if (globalsType != null && !IsValidHostObjectType(globalsType))
            {
                throw new ArgumentException(CodeAnalysisResources.ReturnTypeCannotBeValuePointerbyRefOrOpen, nameof(globalsType));
            }

            if (returnType != null && !IsValidSubmissionReturnType(returnType))
            {
                throw new ArgumentException(CodeAnalysisResources.ReturnTypeCannotBeVoidByRefOrOpen, nameof(returnType));
            }

            if (previousScriptCompilation != null)
            {
                if (globalsType == null)
                {
                    globalsType = previousScriptCompilation.HostObjectType;
                }
                else if (globalsType != previousScriptCompilation.HostObjectType)
                {
                    throw new ArgumentException(CodeAnalysisResources.TypeMustBeSameAsHostObjectTypeOfPreviousSubmission, nameof(globalsType));
                }

                // Force the previous submission to be analyzed. This is required for anonymous types unification.
                if (previousScriptCompilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    throw new InvalidOperationException(CodeAnalysisResources.PreviousSubmissionHasErrors);
                }
            }
        }

        /// <summary>
        /// Checks options passed to submission compilation constructor.
        /// Throws an exception if the options are not applicable to submissions.
        /// </summary>
        internal static void CheckSubmissionOptions(CompilationOptions options)
        {
            if (options == null)
            {
                return;
            }

            if (options.OutputKind.IsValid() && options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidOutputKindForSubmission, nameof(options));
            }

            if (options.CryptoKeyContainer != null ||
                options.CryptoKeyFile != null ||
                options.DelaySign != null ||
                !options.CryptoPublicKey.IsEmpty ||
                (options.DelaySign == true && options.PublicSign))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidCompilationOptions, nameof(options));
            }
        }

        /// <summary>
        /// Creates a new compilation equivalent to this one with different symbol instances.
        /// </summary>
        public Compilation Clone()
        {
            return CommonClone();
        }

        protected abstract Compilation CommonClone();

        /// <summary>
        /// Returns a new compilation with a given event queue.
        /// </summary>
        internal abstract Compilation WithEventQueue(AsyncQueue<CompilationEvent> eventQueue);

        /// <summary>
        /// Gets a new <see cref="SemanticModel"/> for the specified syntax tree.
        /// </summary>
        /// <param name="syntaxTree">The specified syntax tree.</param>
        /// <param name="ignoreAccessibility">
        /// True if the SemanticModel should ignore accessibility rules when answering semantic questions.
        /// </param>
        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility = false)
        {
            return CommonGetSemanticModel(syntaxTree, ignoreAccessibility);
        }

        protected abstract SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility);

        /// <summary>
        /// Returns a new INamedTypeSymbol representing an error type with the given name and arity
        /// in the given optional container.
        /// </summary>
        public abstract INamedTypeSymbol CreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity);

        #region Name

        internal const string UnspecifiedModuleAssemblyName = "?";

        /// <summary>
        /// Simple assembly name, or null if not specified.
        /// </summary>
        /// <remarks>
        /// The name is used for determining internals-visible-to relationship with referenced assemblies.
        /// 
        /// If the compilation represents an assembly the value of <see cref="AssemblyName"/> is its simple name.
        /// 
        /// Unless <see cref="CompilationOptions.ModuleName"/> specifies otherwise the module name
        /// written to metadata is <see cref="AssemblyName"/> with an extension based upon <see cref="CompilationOptions.OutputKind"/>.
        /// </remarks>
        public string AssemblyName { get; }

        internal static void CheckAssemblyName(string assemblyName)
        {
            // We could only allow name == null if OutputKind is Module. 
            // However we couldn't check such condition here since one wouldn't be able to call WithName(...).WithOptions(...).
            // It does no harm that we allow name == null for assemblies as well, so we don't enforce it.

            if (assemblyName != null)
            {
                MetadataHelpers.ValidateAssemblyOrModuleName(assemblyName, "assemblyName");
            }
        }

        internal string MakeSourceAssemblySimpleName()
        {
            return AssemblyName ?? UnspecifiedModuleAssemblyName;
        }

        internal string MakeSourceModuleName()
        {
            return Options.ModuleName ??
                   (AssemblyName != null ? AssemblyName + Options.OutputKind.GetDefaultExtension() : UnspecifiedModuleAssemblyName);
        }

        /// <summary>
        /// Creates a compilation with the specified assembly name.
        /// </summary>
        /// <param name="assemblyName">The new assembly name.</param>
        /// <returns>A new compilation.</returns>
        public Compilation WithAssemblyName(string assemblyName)
        {
            return CommonWithAssemblyName(assemblyName);
        }

        protected abstract Compilation CommonWithAssemblyName(string outputName);

        #endregion

        #region Options

        /// <summary>
        /// Gets the options the compilation was created with.
        /// </summary>
        public CompilationOptions Options { get { return CommonOptions; } }

        protected abstract CompilationOptions CommonOptions { get; }

        /// <summary>
        /// Creates a new compilation with the specified compilation options.
        /// </summary>
        /// <param name="options">The new options.</param>
        /// <returns>A new compilation.</returns>
        public Compilation WithOptions(CompilationOptions options)
        {
            return CommonWithOptions(options);
        }

        protected abstract Compilation CommonWithOptions(CompilationOptions options);

        #endregion

        #region Submissions

        // An index in the submission slot array. Allocated lazily in compilation phase based upon the slot index of the previous submission.
        // Special values:
        // -1 ... neither this nor previous submissions in the chain allocated a slot (the submissions don't contain code)
        // -2 ... the slot of this submission hasn't been determined yet
        // -3 ... this is not a submission compilation
        private int _lazySubmissionSlotIndex;
        private const int SubmissionSlotIndexNotApplicable = -3;
        private const int SubmissionSlotIndexToBeAllocated = -2;

        /// <summary>
        /// True if the compilation represents an interactive submission.
        /// </summary>
        internal bool IsSubmission
        {
            get
            {
                return _lazySubmissionSlotIndex != SubmissionSlotIndexNotApplicable;
            }
        }

        /// <summary>
        /// Gets or allocates a runtime submission slot index for this compilation.
        /// </summary>
        /// <returns>Non-negative integer if this is a submission and it or a previous submission contains code, negative integer otherwise.</returns>
        internal int GetSubmissionSlotIndex()
        {
            if (_lazySubmissionSlotIndex == SubmissionSlotIndexToBeAllocated)
            {
                // TODO (tomat): remove recursion
                int lastSlotIndex = ScriptCompilationInfo.PreviousScriptCompilation?.GetSubmissionSlotIndex() ?? 0;
                _lazySubmissionSlotIndex = HasCodeToEmit() ? lastSlotIndex + 1 : lastSlotIndex;
            }

            return _lazySubmissionSlotIndex;
        }

        // The type of interactive submission result requested by the host, or null if this compilation doesn't represent a submission. 
        //
        // The type is resolved to a symbol when the Script's instance ctor symbol is constructed. The symbol needs to be resolved against
        // the references of this compilation.
        //
        // Consider (tomat): As an alternative to Reflection Type we could hold onto any piece of information that lets us 
        // resolve the type symbol when needed.

        /// <summary>
        /// The type object that represents the type of submission result the host requested.
        /// </summary>
        internal Type SubmissionReturnType => ScriptCompilationInfo?.ReturnType;

        internal static bool IsValidSubmissionReturnType(Type type)
        {
            return !(type == typeof(void) || type.IsByRef || type.GetTypeInfo().ContainsGenericParameters);
        }

        /// <summary>
        /// The type of the globals object or null if not specified for this compilation.
        /// </summary>
        internal Type HostObjectType => ScriptCompilationInfo?.GlobalsType;

        internal static bool IsValidHostObjectType(Type type)
        {
            var info = type.GetTypeInfo();
            return !(info.IsValueType || info.IsPointer || info.IsByRef || info.ContainsGenericParameters);
        }

        internal abstract bool HasSubmissionResult();

        public Compilation WithScriptCompilationInfo(ScriptCompilationInfo info) => CommonWithScriptCompilationInfo(info);
        protected abstract Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo info);

        #endregion

        #region Syntax Trees

        /// <summary>
        /// Gets the syntax trees (parsed from source code) that this compilation was created with.
        /// </summary>
        public IEnumerable<SyntaxTree> SyntaxTrees { get { return CommonSyntaxTrees; } }
        protected abstract IEnumerable<SyntaxTree> CommonSyntaxTrees { get; }

        /// <summary>
        /// Creates a new compilation with additional syntax trees.
        /// </summary>
        /// <param name="trees">The new syntax trees.</param>
        /// <returns>A new compilation.</returns>
        public Compilation AddSyntaxTrees(params SyntaxTree[] trees)
        {
            return CommonAddSyntaxTrees(trees);
        }

        /// <summary>
        /// Creates a new compilation with additional syntax trees.
        /// </summary>
        /// <param name="trees">The new syntax trees.</param>
        /// <returns>A new compilation.</returns>
        public Compilation AddSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            return CommonAddSyntaxTrees(trees);
        }

        protected abstract Compilation CommonAddSyntaxTrees(IEnumerable<SyntaxTree> trees);

        /// <summary>
        /// Creates a new compilation without the specified syntax trees. Preserves metadata info for use with trees
        /// added later.
        /// </summary>
        /// <param name="trees">The new syntax trees.</param>
        /// <returns>A new compilation.</returns>
        public Compilation RemoveSyntaxTrees(params SyntaxTree[] trees)
        {
            return CommonRemoveSyntaxTrees(trees);
        }

        /// <summary>
        /// Creates a new compilation without the specified syntax trees. Preserves metadata info for use with trees
        /// added later.
        /// </summary>
        /// <param name="trees">The new syntax trees.</param>
        /// <returns>A new compilation.</returns>
        public Compilation RemoveSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            return CommonRemoveSyntaxTrees(trees);
        }

        protected abstract Compilation CommonRemoveSyntaxTrees(IEnumerable<SyntaxTree> trees);

        /// <summary>
        /// Creates a new compilation without any syntax trees. Preserves metadata info for use with
        /// trees added later.
        /// </summary>
        public Compilation RemoveAllSyntaxTrees()
        {
            return CommonRemoveAllSyntaxTrees();
        }

        protected abstract Compilation CommonRemoveAllSyntaxTrees();

        /// <summary>
        /// Creates a new compilation with an old syntax tree replaced with a new syntax tree.
        /// Reuses metadata from old compilation object.
        /// </summary>
        /// <param name="newTree">The new tree.</param>
        /// <param name="oldTree">The old tree.</param>
        /// <returns>A new compilation.</returns>
        public Compilation ReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
        {
            return CommonReplaceSyntaxTree(oldTree, newTree);
        }

        protected abstract Compilation CommonReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree);

        /// <summary>
        /// Returns true if this compilation contains the specified tree. False otherwise.
        /// </summary>
        /// <param name="syntaxTree">A syntax tree.</param>
        public bool ContainsSyntaxTree(SyntaxTree syntaxTree)
        {
            return CommonContainsSyntaxTree(syntaxTree);
        }

        protected abstract bool CommonContainsSyntaxTree(SyntaxTree syntaxTree);

        /// <summary>
        /// The event queue that this compilation was created with.
        /// </summary>
        internal readonly AsyncQueue<CompilationEvent> EventQueue;

        #endregion

        #region References

        internal static ImmutableArray<MetadataReference> ValidateReferences<T>(IEnumerable<MetadataReference> references)
            where T : CompilationReference
        {
            var result = references.AsImmutableOrEmpty();
            for (int i = 0; i < result.Length; i++)
            {
                var reference = result[i];
                if (reference == null)
                {
                    throw new ArgumentNullException("references[" + i + "]");
                }

                var peReference = reference as PortableExecutableReference;
                if (peReference == null && !(reference is T))
                {
                    Debug.Assert(reference is UnresolvedMetadataReference || reference is CompilationReference);
                    throw new ArgumentException(String.Format("Reference of type '{0}' is not valid for this compilation.", reference.GetType()), "references[" + i + "]");
                }
            }

            return result;
        }

        internal CommonReferenceManager GetBoundReferenceManager()
        {
            return CommonGetBoundReferenceManager();
        }

        internal abstract CommonReferenceManager CommonGetBoundReferenceManager();

        /// <summary>
        /// Metadata references passed to the compilation constructor.
        /// </summary>
        public ImmutableArray<MetadataReference> ExternalReferences { get; }

        /// <summary>
        /// Unique metadata references specified via #r directive in the source code of this compilation.
        /// </summary>
        public abstract ImmutableArray<MetadataReference> DirectiveReferences { get; }

        /// <summary>
        /// All reference directives used in this compilation.
        /// </summary>
        internal abstract IEnumerable<ReferenceDirective> ReferenceDirectives { get; }

        /// <summary>
        /// Maps values of #r references to resolved metadata references.
        /// </summary>
        internal abstract IDictionary<ValueTuple<string, string>, MetadataReference> ReferenceDirectiveMap { get; }

        /// <summary>
        /// All metadata references -- references passed to the compilation
        /// constructor as well as references specified via #r directives.
        /// </summary>
        public IEnumerable<MetadataReference> References
        {
            get
            {
                foreach (var reference in ExternalReferences)
                {
                    yield return reference;
                }

                foreach (var reference in DirectiveReferences)
                {
                    yield return reference;
                }
            }
        }

        /// <summary>
        /// Creates a metadata reference for this compilation.
        /// </summary>
        /// <param name="aliases">
        /// Optional aliases that can be used to refer to the compilation root namespace via extern alias directive.
        /// </param>
        /// <param name="embedInteropTypes">
        /// Embed the COM types from the reference so that the compiled
        /// application no longer requires a primary interop assembly (PIA).
        /// </param>
        public abstract CompilationReference ToMetadataReference(ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false);

        /// <summary>
        /// Creates a new compilation with the specified references.
        /// </summary>
        /// <param name="newReferences">
        /// The new references.
        /// </param>
        /// <returns>A new compilation.</returns>
        public Compilation WithReferences(IEnumerable<MetadataReference> newReferences)
        {
            return this.CommonWithReferences(newReferences);
        }

        /// <summary>
        /// Creates a new compilation with the specified references.
        /// </summary>
        /// <param name="newReferences">The new references.</param>
        /// <returns>A new compilation.</returns>
        public Compilation WithReferences(params MetadataReference[] newReferences)
        {
            return this.WithReferences((IEnumerable<MetadataReference>)newReferences);
        }

        /// <summary>
        /// Creates a new compilation with the specified references.
        /// </summary>
        protected abstract Compilation CommonWithReferences(IEnumerable<MetadataReference> newReferences);

        /// <summary>
        /// Creates a new compilation with additional metadata references.
        /// </summary>
        /// <param name="references">The new references.</param>
        /// <returns>A new compilation.</returns>
        public Compilation AddReferences(params MetadataReference[] references)
        {
            return AddReferences((IEnumerable<MetadataReference>)references);
        }

        /// <summary>
        /// Creates a new compilation with additional metadata references.
        /// </summary>
        /// <param name="references">The new references.</param>
        /// <returns>A new compilation.</returns>
        public Compilation AddReferences(IEnumerable<MetadataReference> references)
        {
            if (references == null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (references.IsEmpty())
            {
                return this;
            }

            return CommonWithReferences(this.ExternalReferences.Union(references));
        }

        /// <summary>
        /// Creates a new compilation without the specified metadata references.
        /// </summary>
        /// <param name="references">The new references.</param>
        /// <returns>A new compilation.</returns>
        public Compilation RemoveReferences(params MetadataReference[] references)
        {
            return RemoveReferences((IEnumerable<MetadataReference>)references);
        }

        /// <summary>
        /// Creates a new compilation without the specified metadata references.
        /// </summary>
        /// <param name="references">The new references.</param>
        /// <returns>A new compilation.</returns>
        public Compilation RemoveReferences(IEnumerable<MetadataReference> references)
        {
            if (references == null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            if (references.IsEmpty())
            {
                return this;
            }

            var refSet = new HashSet<MetadataReference>(this.ExternalReferences);

            //EDMAURER if AddingReferences accepts duplicates, then a consumer supplying a list with
            //duplicates to add will not know exactly which to remove. Let them supply a list with
            //duplicates here.
            foreach (var r in references.Distinct())
            {
                if (!refSet.Remove(r))
                {
                    throw new ArgumentException($"MetadataReference '{r}' not found to remove", nameof(references));
                }
            }

            return CommonWithReferences(refSet);
        }

        /// <summary>
        /// Creates a new compilation without any metadata references.
        /// </summary>
        public Compilation RemoveAllReferences()
        {
            return CommonWithReferences(SpecializedCollections.EmptyEnumerable<MetadataReference>());
        }

        /// <summary>
        /// Creates a new compilation with an old metadata reference replaced with a new metadata
        /// reference.
        /// </summary>
        /// <param name="newReference">The new reference.</param>
        /// <param name="oldReference">The old reference.</param>
        /// <returns>A new compilation.</returns>
        public Compilation ReplaceReference(MetadataReference oldReference, MetadataReference newReference)
        {
            if (oldReference == null)
            {
                throw new ArgumentNullException(nameof(oldReference));
            }

            if (newReference == null)
            {
                return this.RemoveReferences(oldReference);
            }

            return this.RemoveReferences(oldReference).AddReferences(newReference);
        }

        /// <summary>
        /// Gets the <see cref="IAssemblySymbol"/> or <see cref="IModuleSymbol"/> for a metadata reference used to create this
        /// compilation.
        /// </summary>
        /// <param name="reference">The target reference.</param>
        /// <returns>
        /// Assembly or module symbol corresponding to the given reference or null if there is none.
        /// </returns>
        public ISymbol GetAssemblyOrModuleSymbol(MetadataReference reference)
        {
            return CommonGetAssemblyOrModuleSymbol(reference);
        }

        protected abstract ISymbol CommonGetAssemblyOrModuleSymbol(MetadataReference reference);

        /// <summary>
        /// Gets the <see cref="MetadataReference"/> that corresponds to the assembly symbol. 
        /// </summary>
        /// <param name="assemblySymbol">The target symbol.</param>
        public MetadataReference GetMetadataReference(IAssemblySymbol assemblySymbol)
        {
            return GetBoundReferenceManager().GetMetadataReference(assemblySymbol);
        }

        /// <summary>
        /// Assembly identities of all assemblies directly referenced by this compilation.
        /// </summary>
        /// <remarks>
        /// Includes identities of references passed in the compilation constructor 
        /// as well as those specified via directives in source code. 
        /// </remarks>
        public abstract IEnumerable<AssemblyIdentity> ReferencedAssemblyNames { get; }

        #endregion

        #region Symbols

        /// <summary>
        /// The <see cref="IAssemblySymbol"/> that represents the assembly being created.
        /// </summary>
        public IAssemblySymbol Assembly { get { return CommonAssembly; } }
        protected abstract IAssemblySymbol CommonAssembly { get; }

        /// <summary>
        /// Gets the <see cref="IModuleSymbol"/> for the module being created by compiling all of
        /// the source code.
        /// </summary>
        public IModuleSymbol SourceModule { get { return CommonSourceModule; } }
        protected abstract IModuleSymbol CommonSourceModule { get; }

        /// <summary>
        /// The root namespace that contains all namespaces and types defined in source code or in 
        /// referenced metadata, merged into a single namespace hierarchy.
        /// </summary>
        public INamespaceSymbol GlobalNamespace { get { return CommonGlobalNamespace; } }
        protected abstract INamespaceSymbol CommonGlobalNamespace { get; }

        /// <summary>
        /// Gets the corresponding compilation namespace for the specified module or assembly namespace.
        /// </summary>
        public INamespaceSymbol GetCompilationNamespace(INamespaceSymbol namespaceSymbol)
        {
            return CommonGetCompilationNamespace(namespaceSymbol);
        }

        protected abstract INamespaceSymbol CommonGetCompilationNamespace(INamespaceSymbol namespaceSymbol);

        internal abstract CommonAnonymousTypeManager CommonAnonymousTypeManager { get; }

        /// <summary>
        /// Returns the Main method that will serves as the entry point of the assembly, if it is
        /// executable (and not a script).
        /// </summary>
        public IMethodSymbol GetEntryPoint(CancellationToken cancellationToken)
        {
            return CommonGetEntryPoint(cancellationToken);
        }

        protected abstract IMethodSymbol CommonGetEntryPoint(CancellationToken cancellationToken);

        /// <summary>
        /// Get the symbol for the predefined type from the Cor Library referenced by this
        /// compilation.
        /// </summary>
        public INamedTypeSymbol GetSpecialType(SpecialType specialType)
        {
            return CommonGetSpecialType(specialType);
        }

        /// <summary>
        /// Returns true if the type is System.Type.
        /// </summary>
        internal abstract bool IsSystemTypeReference(ITypeSymbol type);

        protected abstract INamedTypeSymbol CommonGetSpecialType(SpecialType specialType);

        internal abstract ISymbol CommonGetWellKnownTypeMember(WellKnownMember member);

        /// <summary>
        /// Returns true if the specified type is equal to or derives from System.Attribute well-known type.
        /// </summary>
        internal abstract bool IsAttributeType(ITypeSymbol type);

        /// <summary>
        /// The INamedTypeSymbol for the .NET System.Object type, which could have a TypeKind of
        /// Error if there was no COR Library in this Compilation.
        /// </summary>
        public INamedTypeSymbol ObjectType { get { return CommonObjectType; } }
        protected abstract INamedTypeSymbol CommonObjectType { get; }

        /// <summary>
        /// The TypeSymbol for the type 'dynamic' in this Compilation.
        /// </summary>
        public ITypeSymbol DynamicType { get { return CommonDynamicType; } }
        protected abstract ITypeSymbol CommonDynamicType { get; }

        /// <summary>
        /// A symbol representing the implicit Script class. This is null if the class is not
        /// defined in the compilation.
        /// </summary>
        public INamedTypeSymbol ScriptClass { get { return CommonScriptClass; } }
        protected abstract INamedTypeSymbol CommonScriptClass { get; }

        /// <summary>
        /// Returns a new ArrayTypeSymbol representing an array type tied to the base types of the
        /// COR Library in this Compilation.
        /// </summary>
        public IArrayTypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank = 1)
        {
            return CommonCreateArrayTypeSymbol(elementType, rank);
        }

        protected abstract IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank);

        /// <summary>
        /// Returns a new PointerTypeSymbol representing a pointer type tied to a type in this
        /// Compilation.
        /// </summary>
        public IPointerTypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
        {
            return CommonCreatePointerTypeSymbol(pointedAtType);
        }

        protected abstract IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType);

        /// <summary>
        /// Gets the type within the compilation's assembly and all referenced assemblies (other than
        /// those that can only be referenced via an extern alias) using its canonical CLR metadata name.
        /// </summary>
        /// <returns>Null if the type can't be found.</returns>
        /// <remarks>
        /// Since VB does not have the concept of extern aliases, it considers all referenced assemblies.
        /// </remarks>
        public INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            return CommonGetTypeByMetadataName(fullyQualifiedMetadataName);
        }

        protected abstract INamedTypeSymbol CommonGetTypeByMetadataName(string metadataName);

        #endregion

        #region Diagnostics

        internal static readonly CompilationStage DefaultDiagnosticsStage = CompilationStage.Compile;

        /// <summary>
        /// Gets the diagnostics produced during the parsing stage.
        /// </summary>
        public abstract ImmutableArray<Diagnostic> GetParseDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the diagnostics produced during symbol declaration.
        /// </summary>
        public abstract ImmutableArray<Diagnostic> GetDeclarationDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the diagnostics produced during the analysis of method bodies and field initializers.
        /// </summary>
        public abstract ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets all the diagnostics for the compilation, including syntax, declaration, and
        /// binding. Does not include any diagnostics that might be produced during emit, see
        /// <see cref="EmitResult"/>.
        /// </summary>
        public abstract ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

        internal abstract CommonMessageProvider MessageProvider { get; }

        /// <param name="accumulator">Bag to which filtered diagnostics will be added.</param>
        /// <param name="incoming">Diagnostics to be filtered.</param>
        /// <returns>True if there were no errors or warnings-as-errors.</returns>
        internal abstract bool FilterAndAppendAndFreeDiagnostics(DiagnosticBag accumulator, ref DiagnosticBag incoming);

        #endregion

        #region Resources

        /// <summary>
        /// Create a stream filled with default win32 resources.
        /// </summary>
        public Stream CreateDefaultWin32Resources(bool versionResource, bool noManifest, Stream manifestContents, Stream iconInIcoFormat)
        {
            //Win32 resource encodings use a lot of 16bit values. Do all of the math checked with the
            //expectation that integer types are well-chosen with size in mind.
            checked
            {
                var result = new MemoryStream(1024);

                //start with a null resource just as rc.exe does
                AppendNullResource(result);

                if (versionResource)
                    AppendDefaultVersionResource(result);

                if (!noManifest)
                {
                    if (this.Options.OutputKind.IsApplication())
                    {
                        // Applications use a default manifest if one is not specified.
                        if (manifestContents == null)
                        {
                            manifestContents = typeof(Compilation).GetTypeInfo().Assembly.GetManifestResourceStream("Microsoft.CodeAnalysis.Resources.default.win32manifest");
                        }
                    }
                    else
                    {
                        // Modules never have manifests, even if one is specified.
                        //Debug.Assert(!this.Options.OutputKind.IsNetModule() || manifestContents == null);
                    }

                    if (manifestContents != null)
                    {
                        Win32ResourceConversions.AppendManifestToResourceStream(result, manifestContents, !this.Options.OutputKind.IsApplication());
                    }
                }

                if (iconInIcoFormat != null)
                {
                    Win32ResourceConversions.AppendIconToResourceStream(result, iconInIcoFormat);
                }

                result.Position = 0;
                return result;
            }
        }

        internal static void AppendNullResource(Stream resourceStream)
        {
            var writer = new BinaryWriter(resourceStream);
            writer.Write((UInt32)0);
            writer.Write((UInt32)0x20);
            writer.Write((UInt16)0xFFFF);
            writer.Write((UInt16)0);
            writer.Write((UInt16)0xFFFF);
            writer.Write((UInt16)0);
            writer.Write((UInt32)0);            //DataVersion
            writer.Write((UInt16)0);            //MemoryFlags
            writer.Write((UInt16)0);            //LanguageId
            writer.Write((UInt32)0);            //Version 
            writer.Write((UInt32)0);            //Characteristics 
        }

        protected abstract void AppendDefaultVersionResource(Stream resourceStream);

        internal enum Win32ResourceForm : byte
        {
            UNKNOWN,
            COFF,
            RES
        }

        internal Win32ResourceForm DetectWin32ResourceForm(Stream win32Resources)
        {
            var reader = new BinaryReader(win32Resources, Encoding.Unicode);

            var initialPosition = win32Resources.Position;
            var initial32Bits = reader.ReadUInt32();
            win32Resources.Position = initialPosition;

            //RC.EXE output starts with a resource that contains no data.
            if (initial32Bits == 0)
                return Win32ResourceForm.RES;
            else if ((initial32Bits & 0xFFFF0000) != 0 || (initial32Bits & 0x0000FFFF) != 0xFFFF)
                // See CLiteWeightStgdbRW::FindObjMetaData in peparse.cpp
                return Win32ResourceForm.COFF;
            else
                return Win32ResourceForm.UNKNOWN;
        }

        internal Cci.ResourceSection MakeWin32ResourcesFromCOFF(Stream win32Resources, DiagnosticBag diagnostics)
        {
            if (win32Resources == null)
            {
                return null;
            }

            Cci.ResourceSection resources;

            try
            {
                resources = COFFResourceReader.ReadWin32ResourcesFromCOFF(win32Resources);
            }
            catch (BadImageFormatException ex)
            {
                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_BadWin32Resource, Location.None, ex.Message));
                return null;
            }
            catch (IOException ex)
            {
                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_BadWin32Resource, Location.None, ex.Message));
                return null;
            }
            catch (ResourceException ex)
            {
                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_BadWin32Resource, Location.None, ex.Message));
                return null;
            }

            return resources;
        }

        internal List<Win32Resource> MakeWin32ResourceList(Stream win32Resources, DiagnosticBag diagnostics)
        {
            if (win32Resources == null)
            {
                return null;
            }
            List<RESOURCE> resources;

            try
            {
                resources = CvtResFile.ReadResFile(win32Resources);
            }
            catch (ResourceException ex)
            {
                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_BadWin32Resource, Location.None, ex.Message));
                return null;
            }

            if (resources == null)
            {
                return null;
            }

            var resourceList = new List<Win32Resource>();

            foreach (var r in resources)
            {
                var result = new Win32Resource(
                    data: r.data,
                    codePage: 0,
                    languageId: r.LanguageId,
                    //EDMAURER converting to int from ushort. 
                    //Go to short first to avoid sign extension. 
                    id: unchecked((short)r.pstringName.Ordinal),
                    name: r.pstringName.theString,
                    typeId: unchecked((short)r.pstringType.Ordinal),
                    typeName: r.pstringType.theString
                );

                resourceList.Add(result);
            }

            return resourceList;
        }

        internal void ReportManifestResourceDuplicates(
            IEnumerable<ResourceDescription> manifestResources,
            IEnumerable<string> addedModuleNames,
            IEnumerable<string> addedModuleResourceNames,
            DiagnosticBag diagnostics)
        {
            if (Options.OutputKind == OutputKind.NetModule && !(manifestResources != null && manifestResources.Any()))
            {
                return;
            }

            var uniqueResourceNames = new HashSet<string>();

            if (manifestResources != null && manifestResources.Any())
            {
                var uniqueFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var resource in manifestResources)
                {
                    if (!uniqueResourceNames.Add(resource.ResourceName))
                    {
                        diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_ResourceNotUnique, Location.None, resource.ResourceName));
                    }

                    // file name could be null if resource is embedded
                    var fileName = resource.FileName;
                    if (fileName != null && !uniqueFileNames.Add(fileName))
                    {
                        diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_ResourceFileNameNotUnique, Location.None, fileName));
                    }
                }

                foreach (var fileName in addedModuleNames)
                {
                    if (!uniqueFileNames.Add(fileName))
                    {
                        diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_ResourceFileNameNotUnique, Location.None, fileName));
                    }
                }
            }

            if (Options.OutputKind != OutputKind.NetModule)
            {
                foreach (string name in addedModuleResourceNames)
                {
                    if (!uniqueResourceNames.Add(name))
                    {
                        diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_ResourceNotUnique, Location.None, name));
                    }
                }
            }
        }

        #endregion

        #region Emit

        /// <summary>
        /// Constructs the module serialization properties out of the compilation options of this compilation.
        /// </summary>
        internal Cci.ModulePropertiesForSerialization ConstructModuleSerializationProperties(
            EmitOptions emitOptions,
            string targetRuntimeVersion,
            Guid moduleVersionId = default(Guid))
        {
            CompilationOptions compilationOptions = this.Options;
            Platform platform = compilationOptions.Platform;
            OutputKind outputKind = compilationOptions.OutputKind;

            if (!platform.IsValid())
            {
                platform = Platform.AnyCpu;
            }

            if (!outputKind.IsValid())
            {
                outputKind = OutputKind.DynamicallyLinkedLibrary;
            }

            bool requires64Bit = platform.Requires64Bit();
            bool requires32Bit = platform.Requires32Bit();

            ushort fileAlignment;
            if (emitOptions.FileAlignment == 0 || !CompilationOptions.IsValidFileAlignment(emitOptions.FileAlignment))
            {
                fileAlignment = requires64Bit
                    ? Cci.ModulePropertiesForSerialization.DefaultFileAlignment64Bit
                    : Cci.ModulePropertiesForSerialization.DefaultFileAlignment32Bit;
            }
            else
            {
                fileAlignment = (ushort)emitOptions.FileAlignment;
            }

            ulong baseAddress = unchecked(emitOptions.BaseAddress + 0x8000) & (requires64Bit ? 0xffffffffffff0000 : 0x00000000ffff0000);

            // cover values smaller than 0x8000, overflow and default value 0):
            if (baseAddress == 0)
            {
                if (outputKind == OutputKind.ConsoleApplication ||
                    outputKind == OutputKind.WindowsApplication ||
                    outputKind == OutputKind.WindowsRuntimeApplication)
                {
                    baseAddress = (requires64Bit) ? Cci.ModulePropertiesForSerialization.DefaultExeBaseAddress64Bit : Cci.ModulePropertiesForSerialization.DefaultExeBaseAddress32Bit;
                }
                else
                {
                    baseAddress = (requires64Bit) ? Cci.ModulePropertiesForSerialization.DefaultDllBaseAddress64Bit : Cci.ModulePropertiesForSerialization.DefaultDllBaseAddress32Bit;
                }
            }

            ulong sizeOfHeapCommit = requires64Bit
                ? Cci.ModulePropertiesForSerialization.DefaultSizeOfHeapCommit64Bit
                : Cci.ModulePropertiesForSerialization.DefaultSizeOfHeapCommit32Bit;

            // Dev10 always uses the default value for 32bit for sizeOfHeapReserve.
            // check with link -dump -headers <filename>
            const ulong sizeOfHeapReserve = Cci.ModulePropertiesForSerialization.DefaultSizeOfHeapReserve32Bit;

            ulong sizeOfStackReserve = requires64Bit
                ? Cci.ModulePropertiesForSerialization.DefaultSizeOfStackReserve64Bit
                : Cci.ModulePropertiesForSerialization.DefaultSizeOfStackReserve32Bit;

            ulong sizeOfStackCommit = requires64Bit
                ? Cci.ModulePropertiesForSerialization.DefaultSizeOfStackCommit64Bit
                : Cci.ModulePropertiesForSerialization.DefaultSizeOfStackCommit32Bit;

            SubsystemVersion subsystemVersion;
            if (emitOptions.SubsystemVersion.Equals(SubsystemVersion.None) || !emitOptions.SubsystemVersion.IsValid)
            {
                subsystemVersion = SubsystemVersion.Default(outputKind, platform);
            }
            else
            {
                subsystemVersion = emitOptions.SubsystemVersion;
            }

            Machine machine;
            switch (platform)
            {
                case Platform.Arm:
                    machine = Machine.ArmThumb2;
                    break;

                case Platform.X64:
                    machine = Machine.Amd64;
                    break;

                case Platform.Itanium:
                    machine = Machine.IA64;
                    break;

                case Platform.X86:
                    machine = Machine.I386;
                    break;

                case Platform.AnyCpu:
                case Platform.AnyCpu32BitPreferred:
                    machine = Machine.Unknown;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(platform);
            }

            return new Cci.ModulePropertiesForSerialization(
                persistentIdentifier: moduleVersionId,
                fileAlignment: fileAlignment,
                sectionAlignment: Cci.ModulePropertiesForSerialization.DefaultSectionAlignment,
                targetRuntimeVersion: targetRuntimeVersion,
                machine: machine,
                prefer32Bit: platform == Platform.AnyCpu32BitPreferred,
                trackDebugData: false,
                baseAddress: baseAddress,
                sizeOfHeapReserve: sizeOfHeapReserve,
                sizeOfHeapCommit: sizeOfHeapCommit,
                sizeOfStackReserve: sizeOfStackReserve,
                sizeOfStackCommit: sizeOfStackCommit,
                enableHighEntropyVA: emitOptions.HighEntropyVirtualAddressSpace,
                strongNameSigned: HasStrongName,
                imageCharacteristics: GetCharacteristics(outputKind, requires32Bit),
                configureToExecuteInAppContainer: compilationOptions.OutputKind == OutputKind.WindowsRuntimeApplication,
                subsystem: GetSubsystem(outputKind),
                majorSubsystemVersion: (ushort)subsystemVersion.Major,
                minorSubsystemVersion: (ushort)subsystemVersion.Minor,
                linkerMajorVersion: this.LinkerMajorVersion,
                linkerMinorVersion: 0);
        }

        private static Characteristics GetCharacteristics(OutputKind outputKind, bool requires32Bit)
        {
            var characteristics = Characteristics.ExecutableImage;

            if (requires32Bit)
            {
                // 32 bit machine (The standard says to always set this, the linker team says otherwise)
                // The loader team says that this is not used for anything in the OS. 
                characteristics |= Characteristics.Bit32Machine;
            }
            else
            {
                // Large address aware (the standard says never to set this, the linker team says otherwise).
                // The loader team says that this is not overridden for managed binaries and will be respected if set.
                characteristics |= Characteristics.LargeAddressAware;
            }

            switch (outputKind)
            {
                case OutputKind.WindowsRuntimeMetadata:
                case OutputKind.DynamicallyLinkedLibrary:
                case OutputKind.NetModule:
                    characteristics |= Characteristics.Dll;
                    break;

                case OutputKind.ConsoleApplication:
                case OutputKind.WindowsRuntimeApplication:
                case OutputKind.WindowsApplication:
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(outputKind);
            }

            return characteristics;
        }

        private static Subsystem GetSubsystem(OutputKind outputKind)
        {
            switch (outputKind)
            {
                case OutputKind.ConsoleApplication:
                case OutputKind.DynamicallyLinkedLibrary:
                case OutputKind.NetModule:
                case OutputKind.WindowsRuntimeMetadata:
                    return Subsystem.WindowsCui;

                case OutputKind.WindowsRuntimeApplication:
                case OutputKind.WindowsApplication:
                    return Subsystem.WindowsGui;

                default:
                    throw ExceptionUtilities.UnexpectedValue(outputKind);
            }
        }

        /// <summary>
        /// The value is not used by Windows loader, but the OS appcompat infrastructure uses it to identify apps. 
        /// It is useful for us to have a mechanism to identify the compiler that produced the binary. 
        /// This is the appropriate value to use for that. That is what it was invented for. 
        /// We don't want to have the high bit set for this in case some users perform a signed comparison to 
        /// determine if the value is less than some version. The C++ linker is at 0x0B. 
        /// We'll start our numbering at 0x30 for C#, 0x50 for VB.
        /// </summary>
        internal abstract byte LinkerMajorVersion { get; }

        internal bool HasStrongName
        {
            get
            {
                return !IsDelaySigned
                    && Options.OutputKind != OutputKind.NetModule
                    && StrongNameKeys.CanProvideStrongName;
            }
        }

        internal bool IsRealSigned
        {
            get
            {
                // A module cannot be signed. The native compiler allowed one to create a netmodule with an AssemblyKeyFile 
                // or Container attribute (or specify a key via the cmd line). When the module was linked into an assembly,
                // alink would sign the assembly. So rather than give an error we just don't sign when outputting a module.

                return !IsDelaySigned
                    && !Options.PublicSign
                    && Options.OutputKind != OutputKind.NetModule
                    && StrongNameKeys.CanSign;
            }
        }

        /// <summary>
        /// Return true if the compilation contains any code or types.
        /// </summary>
        internal abstract bool HasCodeToEmit();

        internal abstract bool IsDelaySigned { get; }
        internal abstract StrongNameKeys StrongNameKeys { get; }

        internal abstract CommonPEModuleBuilder CreateModuleBuilder(
            EmitOptions emitOptions,
            IMethodSymbol debugEntryPoint,
            IEnumerable<ResourceDescription> manifestResources,
            CompilationTestData testData,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken);

        // TODO: private protected
        internal abstract bool CompileImpl(
            CommonPEModuleBuilder moduleBuilder,
            Stream win32Resources,
            Stream xmlDocStream,
            bool emittingPdb,
            DiagnosticBag diagnostics,
            Predicate<ISymbol> filterOpt,
            CancellationToken cancellationToken);

        internal bool Compile(
            CommonPEModuleBuilder moduleBuilder,
            Stream win32Resources,
            Stream xmlDocStream,
            bool emittingPdb,
            DiagnosticBag diagnostics,
            Predicate<ISymbol> filterOpt,
            CancellationToken cancellationToken)
        {
            try
            {
                return CompileImpl(
                    moduleBuilder,
                    win32Resources,
                    xmlDocStream,
                    emittingPdb,
                    diagnostics,
                    filterOpt,
                    cancellationToken);
            }
            finally
            {
                moduleBuilder.CompilationFinished();
            }
        }

        internal void EnsureAnonymousTypeTemplates(CancellationToken cancellationToken)
        {
            Debug.Assert(IsSubmission);

            if (this.GetSubmissionSlotIndex() >= 0 && HasCodeToEmit())
            {
                if (!this.CommonAnonymousTypeManager.AreTemplatesSealed)
                {
                    var discardedDiagnostics = DiagnosticBag.GetInstance();

                    var moduleBeingBuilt = this.CreateModuleBuilder(
                        emitOptions: EmitOptions.Default,
                        debugEntryPoint: null,
                        manifestResources: null,
                        testData: null,
                        diagnostics: discardedDiagnostics,
                        cancellationToken: cancellationToken);

                    if (moduleBeingBuilt != null)
                    {
                        Compile(
                            moduleBeingBuilt,
                            win32Resources: null,
                            xmlDocStream: null,
                            emittingPdb: false,
                            diagnostics: discardedDiagnostics,
                            filterOpt: null,
                            cancellationToken: cancellationToken);
                    }

                    discardedDiagnostics.Free();
                }

                Debug.Assert(this.CommonAnonymousTypeManager.AreTemplatesSealed);
            }
            else
            {
                this.ScriptCompilationInfo.PreviousScriptCompilation?.EnsureAnonymousTypeTemplates(cancellationToken);
            }
        }

        /// <summary>
        /// Emit the IL for the compiled source code into the specified stream.
        /// </summary>
        /// <param name="peStream">Stream to which the compilation will be written.</param>
        /// <param name="pdbStream">Stream to which the compilation's debug info will be written.  Null to forego PDB generation.</param>
        /// <param name="xmlDocumentationStream">Stream to which the compilation's XML documentation will be written.  Null to forego XML generation.</param>
        /// <param name="win32Resources">Stream from which the compilation's Win32 resources will be read (in RES format).  
        /// Null to indicate that there are none. The RES format begins with a null resource entry.</param>
        /// <param name="manifestResources">List of the compilation's managed resources.  Null to indicate that there are none.</param>
        /// <param name="options">Emit options.</param>
        /// <param name="cancellationToken">To cancel the emit process.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EmitResult Emit(
            Stream peStream,
            Stream pdbStream,
            Stream xmlDocumentationStream,
            Stream win32Resources,
            IEnumerable<ResourceDescription> manifestResources,
            EmitOptions options,
            CancellationToken cancellationToken)
        {
            return Emit(
                peStream,
                pdbStream,
                xmlDocumentationStream,
                win32Resources,
                manifestResources,
                options,
                null,
                cancellationToken);
        }

        /// <summary>
        /// Emit the IL for the compiled source code into the specified stream.
        /// </summary>
        /// <param name="peStream">Stream to which the compilation will be written.</param>
        /// <param name="pdbStream">Stream to which the compilation's debug info will be written.  Null to forego PDB generation.</param>
        /// <param name="xmlDocumentationStream">Stream to which the compilation's XML documentation will be written.  Null to forego XML generation.</param>
        /// <param name="win32Resources">Stream from which the compilation's Win32 resources will be read (in RES format).  
        /// Null to indicate that there are none. The RES format begins with a null resource entry.</param>
        /// <param name="manifestResources">List of the compilation's managed resources.  Null to indicate that there are none.</param>
        /// <param name="options">Emit options.</param>
        /// <param name="debugEntryPoint">
        /// Debug entry-point of the assembly. The method token is stored in the generated PDB stream.
        /// 
        /// When a program launches with a debugger attached the debugger places the first breakpoint to the start of the debug entry-point method.
        /// The CLR starts executing the static Main method of <see cref="CompilationOptions.MainTypeName"/> type. When the first breakpoint is hit
        /// the debugger steps thru the code statement by statement until user code is reached, skipping methods marked by <see cref="DebuggerHiddenAttribute"/>, 
        /// and taking other debugging attributes into consideration.
        /// 
        /// By default both entry points in an executable program (<see cref="OutputKind.ConsoleApplication"/>, <see cref="OutputKind.WindowsApplication"/>, <see cref="OutputKind.WindowsRuntimeApplication"/>)
        /// are the same method (Main). A non-executable program has no entry point. Runtimes that implement a custom loader may specify debug entry-point
        /// to force the debugger to skip over complex custom loader logic executing at the beginning of the .exe and thus improve debugging experience.
        /// 
        /// Unlike ordinary entry-point which is limited to a non-generic static method of specific signature, there are no restrictions on the <paramref name="debugEntryPoint"/> 
        /// method other than having a method body (extern, interface, or abstract methods are not allowed).
        /// </param>
        /// <param name="cancellationToken">To cancel the emit process.</param>
        public EmitResult Emit(
            Stream peStream,
            Stream pdbStream = null,
            Stream xmlDocumentationStream = null,
            Stream win32Resources = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            EmitOptions options = null,
            IMethodSymbol debugEntryPoint = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (peStream == null)
            {
                throw new ArgumentNullException(nameof(peStream));
            }

            if (!peStream.CanWrite)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportWrite, nameof(peStream));
            }

            if (pdbStream != null && !pdbStream.CanWrite)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportWrite, nameof(pdbStream));
            }

            return Emit(
                peStream,
                pdbStream,
                xmlDocumentationStream,
                win32Resources,
                manifestResources,
                options,
                debugEntryPoint,
                testData: null,
                getHostDiagnostics: null,
                cancellationToken: cancellationToken);
        }

        internal EmitResult Emit(
            EmitStreamProvider peStreamProvider,
            EmitStreamProvider pdbStreamProvider,
            EmitStreamProvider xmlDocumentationStreamProvider,
            EmitStreamProvider win32ResourcesProvider,
            IEnumerable<ResourceDescription> manifestResources,
            EmitOptions options,
            IMethodSymbol debugEntryPoint,
            Func<ImmutableArray<Diagnostic>> getHostDiagnostics,
            CancellationToken cancellationToken)
        {
            return Emit(
                peStreamProvider,
                pdbStreamProvider,
                xmlDocumentationStreamProvider,
                win32ResourcesProvider,
                manifestResources,
                options,
                debugEntryPoint,
                testData: null,
                getHostDiagnostics: getHostDiagnostics,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// This overload is only intended to be directly called by tests that want to pass <paramref name="testData"/>.
        /// The map is used for storing a list of methods and their associated IL.
        /// </summary>
        /// <returns>True if emit succeeded.</returns>
        internal EmitResult Emit(
            Stream peStream,
            Stream pdbStream,
            Stream xmlDocumentationStream,
            Stream win32Resources,
            IEnumerable<ResourceDescription> manifestResources,
            EmitOptions options,
            IMethodSymbol debugEntryPoint,
            CompilationTestData testData,
            Func<ImmutableArray<Diagnostic>> getHostDiagnostics,
            CancellationToken cancellationToken)
        {
            return Emit(
                new SimpleEmitStreamProvider(peStream),
                (pdbStream != null) ? new SimpleEmitStreamProvider(pdbStream) : null,
                (xmlDocumentationStream != null) ? new SimpleEmitStreamProvider(xmlDocumentationStream) : null,
                (win32Resources != null) ? new SimpleEmitStreamProvider(win32Resources) : null,
                manifestResources,
                options,
                debugEntryPoint,
                testData,
                getHostDiagnostics,
                cancellationToken);
        }

        /// <summary>
        /// Emit the differences between the compilation and the previous generation
        /// for Edit and Continue. The differences are expressed as added and changed
        /// symbols, and are emitted as metadata, IL, and PDB deltas. A representation
        /// of the current compilation is returned as an EmitBaseline for use in a
        /// subsequent Edit and Continue.
        /// </summary>
        public EmitDifferenceResult EmitDifference(
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<MethodDefinitionHandle> updatedMethods,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return EmitDifference(baseline, edits, s => false, metadataStream, ilStream, pdbStream, updatedMethods, cancellationToken);
        }

        /// <summary>
        /// Emit the differences between the compilation and the previous generation
        /// for Edit and Continue. The differences are expressed as added and changed
        /// symbols, and are emitted as metadata, IL, and PDB deltas. A representation
        /// of the current compilation is returned as an EmitBaseline for use in a
        /// subsequent Edit and Continue.
        /// </summary>
        public EmitDifferenceResult EmitDifference(
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Func<ISymbol, bool> isAddedSymbol,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<MethodDefinitionHandle> updatedMethods,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (baseline == null)
            {
                throw new ArgumentNullException(nameof(baseline));
            }

            // TODO: check if baseline is an assembly manifest module/netmodule
            // Do we support EnC on netmodules?

            if (edits == null)
            {
                throw new ArgumentNullException(nameof(edits));
            }

            if (isAddedSymbol == null)
            {
                throw new ArgumentNullException(nameof(isAddedSymbol));
            }

            if (metadataStream == null)
            {
                throw new ArgumentNullException(nameof(metadataStream));
            }

            if (ilStream == null)
            {
                throw new ArgumentNullException(nameof(ilStream));
            }

            if (pdbStream == null)
            {
                throw new ArgumentNullException(nameof(pdbStream));
            }

            return this.EmitDifference(baseline, edits, isAddedSymbol, metadataStream, ilStream, pdbStream, updatedMethods, null, cancellationToken);
        }

        internal abstract EmitDifferenceResult EmitDifference(
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Func<ISymbol, bool> isAddedSymbol,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<MethodDefinitionHandle> updatedMethodHandles,
            CompilationTestData testData,
            CancellationToken cancellationToken);

        /// <summary>
        /// This overload is only intended to be directly called by tests that want to pass <paramref name="testData"/>.
        /// The map is used for storing a list of methods and their associated IL.
        /// </summary>
        /// <returns>True if emit succeeded.</returns>
        internal EmitResult Emit(
            EmitStreamProvider peStreamProvider,
            EmitStreamProvider pdbStreamProvider,
            EmitStreamProvider xmlDocumentationStreamProvider,
            EmitStreamProvider win32ResourcesStreamProvider,
            IEnumerable<ResourceDescription> manifestResources,
            EmitOptions options,
            IMethodSymbol debugEntryPoint,
            CompilationTestData testData,
            Func<ImmutableArray<Diagnostic>> getHostDiagnostics,
            CancellationToken cancellationToken)
        {
            Debug.Assert(peStreamProvider != null);

            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            if (options != null)
            {
                options.ValidateOptions(diagnostics, this.MessageProvider);
            }
            else
            {
                options = EmitOptions.Default;
            }

            if (debugEntryPoint != null)
            {
                ValidateDebugEntryPoint(debugEntryPoint, diagnostics);
            }

            if (Options.OutputKind == OutputKind.NetModule && manifestResources != null)
            {
                foreach (ResourceDescription res in manifestResources)
                {
                    if (res.FileName != null)
                    {
                        // Modules can have only embedded resources, not linked ones.
                        diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_ResourceInModule, Location.None));
                    }
                }
            }

            if (diagnostics.HasAnyErrors())
            {
                return ToEmitResultAndFree(diagnostics, success: false);
            }

            // Do not waste a slot in the submission chain for submissions that contain no executable code
            // (they may only contain #r directives, usings, etc.)
            if (IsSubmission && !HasCodeToEmit())
            {
                // Still report diagnostics since downstream submissions will assume there are no errors.
                diagnostics.AddRange(this.GetDiagnostics());
                return ToEmitResultAndFree(diagnostics, success: false);
            }

            var moduleBeingBuilt = this.CreateModuleBuilder(
                options,
                debugEntryPoint,
                manifestResources,
                testData,
                diagnostics,
                cancellationToken);

            if (moduleBeingBuilt == null)
            {
                return ToEmitResultAndFree(diagnostics, success: false);
            }

            var win32Resources = win32ResourcesStreamProvider?.GetOrCreateStream(diagnostics);
            var xmlDocumentationStream = xmlDocumentationStreamProvider?.GetOrCreateStream(diagnostics);
            if (!this.Compile(
                moduleBeingBuilt,
                win32Resources,
                xmlDocumentationStream,
                emittingPdb: pdbStreamProvider != null,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: cancellationToken))
            {
                return ToEmitResultAndFree(diagnostics, success: false);
            }

            var hostDiagnostics = getHostDiagnostics?.Invoke() ?? ImmutableArray<Diagnostic>.Empty;

            diagnostics.AddRange(hostDiagnostics);
            if (hostDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
            {
                return ToEmitResultAndFree(diagnostics, success: false);
            }

            bool success = SerializeToPeStream(
                moduleBeingBuilt,
                peStreamProvider,
                pdbStreamProvider,
                testData?.SymWriterFactory,
                diagnostics,
                metadataOnly: options.EmitMetadataOnly,
                cancellationToken: cancellationToken);

            return ToEmitResultAndFree(diagnostics, success);
        }

        internal abstract void ValidateDebugEntryPoint(IMethodSymbol debugEntryPoint, DiagnosticBag diagnostics);

        private static EmitResult ToEmitResultAndFree(DiagnosticBag diagnostics, bool success)
        {
            return new EmitResult(success, diagnostics.ToReadOnlyAndFree());
        }

        internal bool IsEmitDeterministic => this.Options.Deterministic;

        internal bool SerializeToPeStream(
            CommonPEModuleBuilder moduleBeingBuilt,
            EmitStreamProvider peStreamProvider,
            EmitStreamProvider pdbStreamProvider,
            Func<object> testSymWriterFactory,
            DiagnosticBag diagnostics,
            bool metadataOnly,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Cci.PdbWriter nativePdbWriter = null;
            Stream signingInputStream = null;
            DiagnosticBag metadataDiagnostics = null;
            DiagnosticBag pdbBag = null;
            Stream peStream = null;
            Stream portablePdbStream = null;
            Stream portablePdbTempStream = null;
            Stream peTempStream = null;

            bool deterministic = IsEmitDeterministic;
            bool emitPortablePdb = moduleBeingBuilt.EmitOptions.DebugInformationFormat == DebugInformationFormat.PortablePdb;
            string pdbPath = (pdbStreamProvider != null) ? (moduleBeingBuilt.EmitOptions.PdbFilePath ?? FileNameUtilities.ChangeExtension(SourceModule.Name, "pdb")) : null;

            try
            {
                metadataDiagnostics = DiagnosticBag.GetInstance();

                if (!emitPortablePdb && pdbStreamProvider != null)
                {
                    // The calls ISymUnmanagedWriter2.GetDebugInfo require a file name in order to succeed.  This is 
                    // frequently used during PDB writing.  Ensure a name is provided here in the case we were given
                    // only a Stream value.
                    nativePdbWriter = new Cci.PdbWriter(pdbPath, testSymWriterFactory, deterministic);
                }

                Func<Stream> getPortablePdbStream;
                if (emitPortablePdb && pdbStreamProvider != null)
                {
                    getPortablePdbStream = () =>
                    {
                        if (metadataDiagnostics.HasAnyErrors())
                        {
                            return null;
                        }

                        portablePdbStream = pdbStreamProvider.GetOrCreateStream(metadataDiagnostics);
                        if (portablePdbStream == null)
                        {
                            Debug.Assert(metadataDiagnostics.HasAnyErrors());
                            return null;
                        }

                        // When in deterministic mode, we need to seek and read the stream to compute a deterministic PDB ID.
                        // If the underlying stream isn't readable and seekable, we need to use a temp stream.
                        var retStream = portablePdbStream;
                        if (!retStream.CanSeek || deterministic && !retStream.CanRead)
                        {
                            retStream = portablePdbTempStream = new MemoryStream();
                        }

                        return retStream;
                    };
                }
                else
                {
                    getPortablePdbStream = null;
                }

                Func<Stream> getPeStream = () =>
                {
                    if (metadataDiagnostics.HasAnyErrors())
                    {
                        return null;
                    }

                    peStream = peStreamProvider.GetOrCreateStream(metadataDiagnostics);
                    if (peStream == null)
                    {
                        Debug.Assert(metadataDiagnostics.HasAnyErrors());
                        return null;
                    }

                    // Signing can only be done to on-disk files. This is a limitation of the CLR APIs which we use 
                    // to perform strong naming. If this binary is configured to be signed, create a temp file, output to that
                    // then stream that to the stream that this method was called with. Otherwise output to the
                    // stream that this method was called with.
                    Stream retStream;
                    if (!metadataOnly && IsRealSigned)
                    {
                        Debug.Assert(Options.StrongNameProvider != null);

                        // Targeted try-catch for errors during CreateInputStream as found in TFS 1140649
                        // TODO: Put this wrapping in PeWriter to catch all potential PE writing exceptions
                        try
                        {
                            signingInputStream = Options.StrongNameProvider.CreateInputStream();
                            retStream = signingInputStream;
                        }
                        catch (Exception e)
                        {
                            throw new Cci.PeWritingException(e);
                        }
                    }
                    else
                    {
                        signingInputStream = null;
                        retStream = peStream;
                    }

                    // When in deterministic mode, we need to seek and read the stream to compute a deterministic MVID.
                    // If the underlying stream isn't readable and seekable, we need to use a temp stream.
                    if (!retStream.CanSeek || deterministic && !retStream.CanRead)
                    {
                        peTempStream = new MemoryStream();
                        return peTempStream;
                    }

                    return retStream;
                };

                try
                {
                    if (Cci.PeWriter.WritePeToStream(
                        new EmitContext((Cci.IModule)moduleBeingBuilt, null, metadataDiagnostics),
                        this.MessageProvider,
                        getPeStream,
                        getPortablePdbStream,
                        nativePdbWriter,
                        pdbPath,
                        metadataOnly,
                        deterministic,
                        cancellationToken))
                    {
                        if (peTempStream != null)
                        {
                            peTempStream.Position = 0;
                            peTempStream.CopyTo(peStream);
                        }

                        if (portablePdbTempStream != null)
                        {
                            portablePdbTempStream.Position = 0;
                            portablePdbTempStream.CopyTo(portablePdbStream);
                        }

                        if (nativePdbWriter != null)
                        {
                            var nativePdbStream = pdbStreamProvider.GetOrCreateStream(metadataDiagnostics);
                            Debug.Assert(nativePdbStream != null || metadataDiagnostics.HasAnyErrors());

                            if (nativePdbStream != null)
                            {
                                nativePdbWriter.WriteTo(nativePdbStream);
                            }
                        }
                    }
                }
                catch (Cci.PdbWritingException ex)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PdbWritingFailed, Location.None, ex.Message));
                    return false;
                }
                catch (Cci.PeWritingException e)
                {
                    // Targeted fix for TFS 1140649
                    // TODO: Add resource and better error message for a variety of PE exceptions
                    diagnostics.Add(StrongNameKeys.GetError(StrongNameKeys.KeyFilePath, StrongNameKeys.KeyContainer, e.Message, MessageProvider));
                }
                catch (ResourceException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_CantReadResource, Location.None, e.Message, e.InnerException.Message));
                    return false;
                }
                catch (PermissionSetFileReadException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PermissionSetAttributeFileReadError, Location.None, e.FileName, e.PropertyName, e.Message));
                    return false;
                }

                // translate metadata errors.
                if (!FilterAndAppendAndFreeDiagnostics(diagnostics, ref metadataDiagnostics))
                {
                    return false;
                }

                if (signingInputStream != null && peStream != null)
                {
                    Debug.Assert(Options.StrongNameProvider != null);

                    try
                    {
                        Options.StrongNameProvider.SignAssembly(StrongNameKeys, signingInputStream, peStream);
                    }
                    catch (IOException ex)
                    {
                        diagnostics.Add(StrongNameKeys.GetError(StrongNameKeys.KeyFilePath, StrongNameKeys.KeyContainer, ex.Message, MessageProvider));
                        return false;
                    }
                }
            }
            finally
            {
                nativePdbWriter?.Dispose();
                peTempStream?.Dispose();
                portablePdbTempStream?.Dispose();
                signingInputStream?.Dispose();
                pdbBag?.Free();
                metadataDiagnostics?.Free();
            }

            return true;
        }

        internal EmitBaseline SerializeToDeltaStreams(
            CommonPEModuleBuilder moduleBeingBuilt,
            EmitBaseline baseline,
            DefinitionMap definitionMap,
            SymbolChanges changes,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<MethodDefinitionHandle> updatedMethods,
            DiagnosticBag diagnostics,
            Func<object> testSymWriterFactory,
            CancellationToken cancellationToken)
        {
            using (var pdbWriter = new Cci.PdbWriter(
                moduleBeingBuilt.EmitOptions.PdbFilePath ?? FileNameUtilities.ChangeExtension(SourceModule.Name, "pdb"),
                testSymWriterFactory,
                deterministic: false))
            {
                var context = new EmitContext((Cci.IModule)moduleBeingBuilt, null, diagnostics);
                var encId = Guid.NewGuid();

                try
                {
                    var writer = new DeltaMetadataWriter(
                        context,
                        MessageProvider,
                        baseline,
                        encId,
                        definitionMap,
                        changes,
                        cancellationToken);

                    Cci.MetadataSizes metadataSizes;
                    writer.WriteMetadataAndIL(pdbWriter, metadataStream, ilStream, out metadataSizes);
                    writer.GetMethodTokens(updatedMethods);

                    pdbWriter.WriteTo(pdbStream);

                    return diagnostics.HasAnyErrors() ? null : writer.GetDelta(baseline, this, encId, metadataSizes);
                }
                catch (Cci.PdbWritingException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PdbWritingFailed, Location.None, e.Message));
                    return null;
                }
                catch (PermissionSetFileReadException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PermissionSetAttributeFileReadError, Location.None, e.FileName, e.PropertyName, e.Message));
                    return null;
                }
            }
        }

        internal string Feature(string p)
        {
            string v;
            return _features.TryGetValue(p, out v) ? v : null;
        }

        #endregion

        private ConcurrentDictionary<SyntaxTree, SmallConcurrentSetOfInts> _lazyTreeToUsedImportDirectivesMap;
        private static readonly Func<SyntaxTree, SmallConcurrentSetOfInts> s_createSetCallback = t => new SmallConcurrentSetOfInts();

        private ConcurrentDictionary<SyntaxTree, SmallConcurrentSetOfInts> TreeToUsedImportDirectivesMap
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _lazyTreeToUsedImportDirectivesMap);
            }
        }

        internal void MarkImportDirectiveAsUsed(SyntaxNode node)
        {
            MarkImportDirectiveAsUsed(node.SyntaxTree, node.Span.Start);
        }

        internal void MarkImportDirectiveAsUsed(SyntaxTree syntaxTree, int position)
        {
            // Optimization: Don't initialize TreeToUsedImportDirectivesMap in submissions.
            if (!IsSubmission && syntaxTree != null)
            {
                var set = TreeToUsedImportDirectivesMap.GetOrAdd(syntaxTree, s_createSetCallback);
                set.Add(position);
            }
        }

        internal bool IsImportDirectiveUsed(SyntaxTree syntaxTree, int position)
        {
            if (IsSubmission)
            {
                // Since usings apply to subsequent submissions, we have to assume they are used.
                return true;
            }

            SmallConcurrentSetOfInts usedImports;
            return syntaxTree != null &&
                TreeToUsedImportDirectivesMap.TryGetValue(syntaxTree, out usedImports) &&
                usedImports.Contains(position);
        }

        /// <summary>
        /// The compiler needs to define an ordering among different partial class in different syntax trees
        /// in some cases, because emit order for fields in structures, for example, is semantically important.
        /// This function defines an ordering among syntax trees in this compilation.
        /// </summary>
        internal int CompareSyntaxTreeOrdering(SyntaxTree tree1, SyntaxTree tree2)
        {
            if (tree1 == tree2)
            {
                return 0;
            }

            Debug.Assert(this.ContainsSyntaxTree(tree1));
            Debug.Assert(this.ContainsSyntaxTree(tree2));

            return this.GetSyntaxTreeOrdinal(tree1) - this.GetSyntaxTreeOrdinal(tree2);
        }

        internal abstract int GetSyntaxTreeOrdinal(SyntaxTree tree);

        /// <summary>
        /// Compare two source locations, using their containing trees, and then by Span.First within a tree. 
        /// Can be used to get a total ordering on declarations, for example.
        /// </summary>
        internal abstract int CompareSourceLocations(Location loc1, Location loc2);

        /// <summary>
        /// Return the lexically first of two locations.
        /// </summary>
        internal TLocation FirstSourceLocation<TLocation>(TLocation first, TLocation second)
            where TLocation : Location
        {
            if (CompareSourceLocations(first, second) <= 0)
            {
                return first;
            }
            else
            {
                return second;
            }
        }

        /// <summary>
        /// Return the lexically first of multiple locations.
        /// </summary>
        internal TLocation FirstSourceLocation<TLocation>(ImmutableArray<TLocation> locations)
            where TLocation : Location
        {
            if (locations.IsEmpty)
            {
                return null;
            }

            var result = locations[0];

            for (int i = 1; i < locations.Length; i++)
            {
                result = FirstSourceLocation(result, locations[i]);
            }

            return result;
        }

        #region Logging Helpers

        // Following helpers are used when logging ETW events. These helpers are invoked only if we are running
        // under an ETW listener that has requested 'verbose' logging. In other words, these helpers will never
        // be invoked in the 'normal' case (i.e. when the code is running on user's machine and no ETW listener
        // is involved).

        // Note: Most of the below helpers are unused at the moment - but we would like to keep them around in
        // case we decide we need more verbose logging in certain cases for debugging.
        internal string GetMessage(CompilationStage stage)
        {
            return string.Format("{0} ({1})", this.AssemblyName, stage.ToString());
        }

        internal string GetMessage(ITypeSymbol source, ITypeSymbol destination)
        {
            if (source == null || destination == null) return this.AssemblyName;
            return string.Format("{0}: {1} {2} -> {3} {4}", this.AssemblyName, source.TypeKind.ToString(), source.Name, destination.TypeKind.ToString(), destination.Name);
        }

        #endregion

        #region Declaration Name Queries

        /// <summary>
        /// Return true if there is a source declaration symbol name that meets given predicate.
        /// </summary>
        public abstract bool ContainsSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Return source declaration symbols whose name meets given predicate.
        /// </summary>
        public abstract IEnumerable<ISymbol> GetSymbolsWithName(Func<string, bool> predicate, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken));

        #endregion

        internal void MakeMemberMissing(WellKnownMember member)
        {
            MakeMemberMissing((int)member);
        }

        internal void MakeMemberMissing(SpecialMember member)
        {
            MakeMemberMissing(-(int)member - 1);
        }

        internal bool IsMemberMissing(WellKnownMember member)
        {
            return IsMemberMissing((int)member);
        }

        internal bool IsMemberMissing(SpecialMember member)
        {
            return IsMemberMissing(-(int)member - 1);
        }

        private void MakeMemberMissing(int member)
        {
            if (_lazyMakeMemberMissingMap == null)
            {
                _lazyMakeMemberMissingMap = new SmallDictionary<int, bool>();
            }

            _lazyMakeMemberMissingMap[member] = true;
        }

        private bool IsMemberMissing(int member)
        {
            return _lazyMakeMemberMissingMap != null && _lazyMakeMemberMissingMap.ContainsKey(member);
        }

        internal void MakeTypeMissing(WellKnownType type)
        {
            if (_lazyMakeWellKnownTypeMissingMap == null)
            {
                _lazyMakeWellKnownTypeMissingMap = new SmallDictionary<int, bool>();
            }

            _lazyMakeWellKnownTypeMissingMap[(int)type] = true;
        }

        internal bool IsTypeMissing(WellKnownType type)
        {
            return _lazyMakeWellKnownTypeMissingMap != null && _lazyMakeWellKnownTypeMissingMap.ContainsKey((int)type);
        }
    }
}

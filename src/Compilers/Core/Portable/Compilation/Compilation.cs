﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.DiaSymReader;
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
        private SmallDictionary<int, bool>? _lazyMakeWellKnownTypeMissingMap;

        /// <summary>
        /// Used for test purposes only to emulate missing members.
        /// </summary>
        private SmallDictionary<int, bool>? _lazyMakeMemberMissingMap;

        // Protected for access in CSharpCompilation.WithAdditionalFeatures
        protected readonly IReadOnlyDictionary<string, string> _features;

        public ScriptCompilationInfo? ScriptCompilationInfo => CommonScriptCompilationInfo;
        internal abstract ScriptCompilationInfo? CommonScriptCompilationInfo { get; }

        internal Compilation(
            string? name,
            ImmutableArray<MetadataReference> references,
            IReadOnlyDictionary<string, string> features,
            bool isSubmission,
            AsyncQueue<CompilationEvent>? eventQueue)
        {
            RoslynDebug.Assert(!references.IsDefault);
            RoslynDebug.Assert(features != null);

            this.AssemblyName = name;
            this.ExternalReferences = references;
            this.EventQueue = eventQueue;

            _lazySubmissionSlotIndex = isSubmission ? SubmissionSlotIndexToBeAllocated : SubmissionSlotIndexNotApplicable;
            _features = features;
        }

        protected static IReadOnlyDictionary<string, string> SyntaxTreeCommonFeatures(IEnumerable<SyntaxTree> trees)
        {
            IReadOnlyDictionary<string, string>? set = null;

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
                        throw new ArgumentException(CodeAnalysisResources.InconsistentSyntaxTreeFeature, nameof(trees));
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

        internal abstract AnalyzerDriver CreateAnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager, SeverityFilter severityFilter);

        /// <summary>
        /// Gets the source language ("C#" or "Visual Basic").
        /// </summary>
        public abstract string Language { get; }

        internal abstract void SerializePdbEmbeddedCompilationOptions(BlobBuilder builder);

        internal static void ValidateScriptCompilationParameters(Compilation? previousScriptCompilation, Type? returnType, ref Type? globalsType)
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
        internal static void CheckSubmissionOptions(CompilationOptions? options)
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
        internal abstract Compilation WithEventQueue(AsyncQueue<CompilationEvent>? eventQueue);

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
        public INamedTypeSymbol CreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (arity < 0)
            {
                throw new ArgumentException($"{nameof(arity)} must be >= 0", nameof(arity));
            }

            return CommonCreateErrorTypeSymbol(container, name, arity);
        }

        protected abstract INamedTypeSymbol CommonCreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity);

        /// <summary>
        /// Returns a new INamespaceSymbol representing an error (missing) namespace with the given name.
        /// </summary>
        public INamespaceSymbol CreateErrorNamespaceSymbol(INamespaceSymbol container, string name)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return CommonCreateErrorNamespaceSymbol(container, name);
        }

        protected abstract INamespaceSymbol CommonCreateErrorNamespaceSymbol(INamespaceSymbol container, string name);

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
        public string? AssemblyName { get; }

        internal void CheckAssemblyName(DiagnosticBag diagnostics)
        {
            // We could only allow name == null if OutputKind is Module.
            // However, it does no harm that we allow name == null for assemblies as well, so we don't enforce it.

            if (this.AssemblyName != null)
            {
                MetadataHelpers.CheckAssemblyOrModuleName(this.AssemblyName, MessageProvider, MessageProvider.ERR_BadAssemblyName, diagnostics);
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
        public Compilation WithAssemblyName(string? assemblyName)
        {
            return CommonWithAssemblyName(assemblyName);
        }

        protected abstract Compilation CommonWithAssemblyName(string? outputName);

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
        /// The previous submission, if any, or null.
        /// </summary>
        private Compilation? PreviousSubmission
        {
            get
            {
                return ScriptCompilationInfo?.PreviousScriptCompilation;
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
                int lastSlotIndex = ScriptCompilationInfo!.PreviousScriptCompilation?.GetSubmissionSlotIndex() ?? 0;
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
        internal Type? SubmissionReturnType => ScriptCompilationInfo?.ReturnTypeOpt;

        internal static bool IsValidSubmissionReturnType(Type type)
        {
            return !(type == typeof(void) || type.IsByRef || type.GetTypeInfo().ContainsGenericParameters);
        }

        /// <summary>
        /// The type of the globals object or null if not specified for this compilation.
        /// </summary>
        internal Type? HostObjectType => ScriptCompilationInfo?.GlobalsType;

        internal static bool IsValidHostObjectType(Type type)
        {
            var info = type.GetTypeInfo();
            return !(info.IsValueType || info.IsPointer || info.IsByRef || info.ContainsGenericParameters);
        }

        internal abstract bool HasSubmissionResult();

        public Compilation WithScriptCompilationInfo(ScriptCompilationInfo? info) => CommonWithScriptCompilationInfo(info);
        protected abstract Compilation CommonWithScriptCompilationInfo(ScriptCompilationInfo? info);

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

        protected abstract bool CommonContainsSyntaxTree(SyntaxTree? syntaxTree);

        /// <summary>
        /// The event queue that this compilation was created with.
        /// </summary>
        internal readonly AsyncQueue<CompilationEvent>? EventQueue;

        #endregion

        #region References

        internal static ImmutableArray<MetadataReference> ValidateReferences<T>(IEnumerable<MetadataReference>? references)
            where T : CompilationReference
        {
            var result = references.AsImmutableOrEmpty();
            for (int i = 0; i < result.Length; i++)
            {
                var reference = result[i];
                if (reference == null)
                {
                    throw new ArgumentNullException($"{nameof(references)}[{i}]");
                }

                var peReference = reference as PortableExecutableReference;
                if (peReference == null && !(reference is T))
                {
                    Debug.Assert(reference is UnresolvedMetadataReference || reference is CompilationReference);
                    throw new ArgumentException(string.Format(CodeAnalysisResources.ReferenceOfTypeIsInvalid1, reference.GetType()),
                                    $"{nameof(references)}[{i}]");
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
        internal abstract IDictionary<(string path, string content), MetadataReference> ReferenceDirectiveMap { get; }

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
                    throw new ArgumentException(string.Format(CodeAnalysisResources.MetadataRefNotFoundToRemove1, r),
                                nameof(references));
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
        public Compilation ReplaceReference(MetadataReference oldReference, MetadataReference? newReference)
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
        public ISymbol? GetAssemblyOrModuleSymbol(MetadataReference reference)
        {
            return CommonGetAssemblyOrModuleSymbol(reference);
        }

        protected abstract ISymbol? CommonGetAssemblyOrModuleSymbol(MetadataReference reference);

        /// <summary>
        /// Gets the <see cref="MetadataReference"/> that corresponds to the assembly symbol.
        /// </summary>
        /// <param name="assemblySymbol">The target symbol.</param>
        public MetadataReference? GetMetadataReference(IAssemblySymbol assemblySymbol)
        {
            return CommonGetMetadataReference(assemblySymbol);
        }

        private protected abstract MetadataReference? CommonGetMetadataReference(IAssemblySymbol assemblySymbol);

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
        public INamespaceSymbol? GetCompilationNamespace(INamespaceSymbol namespaceSymbol)
        {
            return CommonGetCompilationNamespace(namespaceSymbol);
        }

        protected abstract INamespaceSymbol? CommonGetCompilationNamespace(INamespaceSymbol namespaceSymbol);

        internal abstract CommonAnonymousTypeManager CommonAnonymousTypeManager { get; }

        /// <summary>
        /// Returns the Main method that will serves as the entry point of the assembly, if it is
        /// executable (and not a script).
        /// </summary>
        public IMethodSymbol? GetEntryPoint(CancellationToken cancellationToken)
        {
            return CommonGetEntryPoint(cancellationToken);
        }

        protected abstract IMethodSymbol? CommonGetEntryPoint(CancellationToken cancellationToken);

        /// <summary>
        /// Get the symbol for the predefined type from the Cor Library referenced by this
        /// compilation.
        /// </summary>
        public INamedTypeSymbol GetSpecialType(SpecialType specialType)
        {
            return (INamedTypeSymbol)CommonGetSpecialType(specialType).GetITypeSymbol();
        }

        /// <summary>
        /// Get the symbol for the predefined type member from the COR Library referenced by this compilation.
        /// </summary>
        internal abstract ISymbolInternal CommonGetSpecialTypeMember(SpecialMember specialMember);

        /// <summary>
        /// Returns true if the type is System.Type.
        /// </summary>
        internal abstract bool IsSystemTypeReference(ITypeSymbolInternal type);

        private protected abstract INamedTypeSymbolInternal CommonGetSpecialType(SpecialType specialType);

        /// <summary>
        /// Lookup member declaration in well known type used by this Compilation.
        /// </summary>
        internal abstract ISymbolInternal? CommonGetWellKnownTypeMember(WellKnownMember member);

        /// <summary>
        /// Lookup well-known type used by this Compilation.
        /// </summary>
        internal abstract ITypeSymbolInternal CommonGetWellKnownType(WellKnownType wellknownType);

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
        /// <exception cref="NotSupportedException">If the compilation is a VisualBasic compilation.</exception>
        public ITypeSymbol DynamicType { get { return CommonDynamicType; } }
        protected abstract ITypeSymbol CommonDynamicType { get; }

        /// <summary>
        /// A symbol representing the implicit Script class. This is null if the class is not
        /// defined in the compilation.
        /// </summary>
        public INamedTypeSymbol? ScriptClass { get { return CommonScriptClass; } }
        protected abstract INamedTypeSymbol? CommonScriptClass { get; }

        /// <summary>
        /// Resolves a symbol that represents script container (Script class). Uses the
        /// full name of the container class stored in <see cref="CompilationOptions.ScriptClassName"/> to find the symbol.
        /// </summary>
        /// <returns>The Script class symbol or null if it is not defined.</returns>
        protected INamedTypeSymbol? CommonBindScriptClass()
        {
            string scriptClassName = this.Options.ScriptClassName ?? "";

            string[] parts = scriptClassName.Split('.');
            INamespaceSymbol container = this.SourceModule.GlobalNamespace;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                INamespaceSymbol? next = container.GetNestedNamespace(parts[i]);
                if (next == null)
                {
                    AssertNoScriptTrees();
                    return null;
                }

                container = next;
            }

            foreach (INamedTypeSymbol candidate in container.GetTypeMembers(parts[parts.Length - 1]))
            {
                if (candidate.IsScriptClass)
                {
                    return candidate;
                }
            }

            AssertNoScriptTrees();
            return null;
        }

        [Conditional("DEBUG")]
        private void AssertNoScriptTrees()
        {
            foreach (var tree in this.SyntaxTrees)
            {
                Debug.Assert(tree.Options.Kind != SourceCodeKind.Script);
            }
        }

        /// <summary>
        /// Returns a new ArrayTypeSymbol representing an array type tied to the base types of the
        /// COR Library in this Compilation.
        /// </summary>
        public IArrayTypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank = 1, NullableAnnotation elementNullableAnnotation = NullableAnnotation.None)
        {
            return CommonCreateArrayTypeSymbol(elementType, rank, elementNullableAnnotation);
        }

        /// <summary>
        /// Returns a new ArrayTypeSymbol representing an array type tied to the base types of the
        /// COR Library in this Compilation.
        /// </summary>
        /// <remarks>This overload is for backwards compatibility. Do not remove.</remarks>
        public IArrayTypeSymbol CreateArrayTypeSymbol(ITypeSymbol elementType, int rank)
        {
            return CreateArrayTypeSymbol(elementType, rank, elementNullableAnnotation: default);
        }

        protected abstract IArrayTypeSymbol CommonCreateArrayTypeSymbol(ITypeSymbol elementType, int rank, NullableAnnotation elementNullableAnnotation);

        /// <summary>
        /// Returns a new IPointerTypeSymbol representing a pointer type tied to a type in this
        /// Compilation.
        /// </summary>
        /// <exception cref="NotSupportedException">If the compilation is a VisualBasic compilation.</exception>
        public IPointerTypeSymbol CreatePointerTypeSymbol(ITypeSymbol pointedAtType)
        {
            return CommonCreatePointerTypeSymbol(pointedAtType);
        }

        protected abstract IPointerTypeSymbol CommonCreatePointerTypeSymbol(ITypeSymbol elementType);


        /// <summary>
        /// Returns a new IFunctionPointerTypeSymbol representing a function pointer type tied to types in this
        /// Compilation.
        /// </summary>
        /// <exception cref="NotSupportedException">If the compilation is a VisualBasic compilation.</exception>
        /// <exception cref="ArgumentException">
        /// If:
        ///  * <see cref="RefKind.Out"/> is passed as the returnRefKind.
        ///  * parameterTypes and parameterRefKinds do not have the same length.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If returnType is <see langword="null"/>, or if parameterTypes or parameterRefKinds are default,
        /// or if any of the types in parameterTypes are null.</exception>
         // https://github.com/dotnet/roslyn/issues/39865 allow setting calling convention in creation
        public IFunctionPointerTypeSymbol CreateFunctionPointerTypeSymbol(ITypeSymbol returnType, RefKind returnRefKind, ImmutableArray<ITypeSymbol> parameterTypes, ImmutableArray<RefKind> parameterRefKinds)
        {
            return CommonCreateFunctionPointerTypeSymbol(returnType, returnRefKind, parameterTypes, parameterRefKinds);
        }

        protected abstract IFunctionPointerTypeSymbol CommonCreateFunctionPointerTypeSymbol(ITypeSymbol returnType, RefKind returnRefKind, ImmutableArray<ITypeSymbol> parameterTypes, ImmutableArray<RefKind> parameterRefKinds);

        /// <summary>
        /// Returns a new INamedTypeSymbol representing a native integer.
        /// </summary>
        /// <exception cref="NotSupportedException">If the compilation is a VisualBasic compilation.</exception>
        public INamedTypeSymbol CreateNativeIntegerTypeSymbol(bool signed)
        {
            return CommonCreateNativeIntegerTypeSymbol(signed);
        }

        protected abstract INamedTypeSymbol CommonCreateNativeIntegerTypeSymbol(bool signed);

        // PERF: ETW Traces show that analyzers may use this method frequently, often requesting
        // the same symbol over and over again. XUnit analyzers, in particular, were consuming almost
        // 1% of CPU time when building Roslyn itself. This is an extremely simple cache that evicts on
        // hash code conflicts, but seems to do the trick. The size is mostly arbitrary. My guess
        // is that there are maybe a couple dozen analyzers in the solution and each one has
        // ~0-2 unique well-known types, and the chance of hash collision is very low.
        private ConcurrentCache<string, INamedTypeSymbol?> _getTypeCache =
            new ConcurrentCache<string, INamedTypeSymbol?>(50, ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Gets the type within the compilation's assembly and all referenced assemblies (other than
        /// those that can only be referenced via an extern alias) using its canonical CLR metadata name.
        /// </summary>
        /// <returns>Null if the type can't be found.</returns>
        /// <remarks>
        /// Since VB does not have the concept of extern aliases, it considers all referenced assemblies.
        /// </remarks>
        public INamedTypeSymbol? GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            if (!_getTypeCache.TryGetValue(fullyQualifiedMetadataName, out INamedTypeSymbol? val))
            {
                val = CommonGetTypeByMetadataName(fullyQualifiedMetadataName);
                // Ignore if someone added the same value before us
                _ = _getTypeCache.TryAdd(fullyQualifiedMetadataName, val);
            }
            return val;
        }

        protected abstract INamedTypeSymbol? CommonGetTypeByMetadataName(string metadataName);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Returns a new INamedTypeSymbol with the given element types and
        /// (optional) element names, locations, and nullable annotations.
        /// </summary>
        public INamedTypeSymbol CreateTupleTypeSymbol(
            ImmutableArray<ITypeSymbol> elementTypes,
            ImmutableArray<string?> elementNames = default,
            ImmutableArray<Location?> elementLocations = default,
            ImmutableArray<NullableAnnotation> elementNullableAnnotations = default)
        {
            if (elementTypes.IsDefault)
            {
                throw new ArgumentNullException(nameof(elementTypes));
            }

            int n = elementTypes.Length;
            if (elementTypes.Length <= 1)
            {
                throw new ArgumentException(CodeAnalysisResources.TuplesNeedAtLeastTwoElements, nameof(elementNames));
            }

            elementNames = CheckTupleElementNames(n, elementNames);
            CheckTupleElementLocations(n, elementLocations);
            CheckTupleElementNullableAnnotations(n, elementNullableAnnotations);

            for (int i = 0; i < n; i++)
            {
                if (elementTypes[i] == null)
                {
                    throw new ArgumentNullException($"{nameof(elementTypes)}[{i}]");
                }

                if (!elementLocations.IsDefault && elementLocations[i] == null)
                {
                    throw new ArgumentNullException($"{nameof(elementLocations)}[{i}]");
                }
            }

            return CommonCreateTupleTypeSymbol(elementTypes, elementNames, elementLocations, elementNullableAnnotations);
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Returns a new INamedTypeSymbol with the given element types, names, and locations.
        /// </summary>
        /// <remarks>This overload is for backwards compatibility. Do not remove.</remarks>
        public INamedTypeSymbol CreateTupleTypeSymbol(
            ImmutableArray<ITypeSymbol> elementTypes,
            ImmutableArray<string?> elementNames,
            ImmutableArray<Location?> elementLocations)
        {
            return CreateTupleTypeSymbol(elementTypes, elementNames, elementLocations, elementNullableAnnotations: default);
        }

        protected static void CheckTupleElementNullableAnnotations(
            int cardinality,
            ImmutableArray<NullableAnnotation> elementNullableAnnotations)
        {
            if (!elementNullableAnnotations.IsDefault)
            {
                if (elementNullableAnnotations.Length != cardinality)
                {
                    throw new ArgumentException(CodeAnalysisResources.TupleElementNullableAnnotationCountMismatch, nameof(elementNullableAnnotations));
                }
            }
        }

        /// <summary>
        /// Check that if any names are provided, and their number matches the expected cardinality.
        /// Returns a normalized version of the element names (empty array if all the names are null).
        /// </summary>
        protected static ImmutableArray<string?> CheckTupleElementNames(int cardinality, ImmutableArray<string?> elementNames)
        {
            if (!elementNames.IsDefault)
            {
                if (elementNames.Length != cardinality)
                {
                    throw new ArgumentException(CodeAnalysisResources.TupleElementNameCountMismatch, nameof(elementNames));
                }

                for (int i = 0; i < elementNames.Length; i++)
                {
                    if (elementNames[i] == "")
                    {
                        throw new ArgumentException(CodeAnalysisResources.TupleElementNameEmpty, $"{nameof(elementNames)}[{i}]");
                    }
                }

                if (elementNames.All(n => n == null))
                {
                    return default;
                }
            }

            return elementNames;
        }

        protected static void CheckTupleElementLocations(
            int cardinality,
            ImmutableArray<Location?> elementLocations)
        {
            if (!elementLocations.IsDefault)
            {
                if (elementLocations.Length != cardinality)
                {
                    throw new ArgumentException(CodeAnalysisResources.TupleElementLocationCountMismatch, nameof(elementLocations));
                }
            }
        }

        protected abstract INamedTypeSymbol CommonCreateTupleTypeSymbol(
            ImmutableArray<ITypeSymbol> elementTypes,
            ImmutableArray<string?> elementNames,
            ImmutableArray<Location?> elementLocations,
            ImmutableArray<NullableAnnotation> elementNullableAnnotations);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Returns a new INamedTypeSymbol with the given underlying type and
        /// (optional) element names, locations, and nullable annotations.
        /// The underlying type needs to be tuple-compatible.
        /// </summary>
        public INamedTypeSymbol CreateTupleTypeSymbol(
            INamedTypeSymbol underlyingType,
            ImmutableArray<string?> elementNames = default,
            ImmutableArray<Location?> elementLocations = default,
            ImmutableArray<NullableAnnotation> elementNullableAnnotations = default)
        {
            if ((object)underlyingType == null)
            {
                throw new ArgumentNullException(nameof(underlyingType));
            }

            return CommonCreateTupleTypeSymbol(underlyingType, elementNames, elementLocations, elementNullableAnnotations);
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Returns a new INamedTypeSymbol with the given underlying type and element names and locations.
        /// The underlying type needs to be tuple-compatible.
        /// </summary>
        /// <remarks>This overload is for backwards compatibility. Do not remove.</remarks>
        public INamedTypeSymbol CreateTupleTypeSymbol(
            INamedTypeSymbol underlyingType,
            ImmutableArray<string?> elementNames,
            ImmutableArray<Location?> elementLocations)
        {
            return CreateTupleTypeSymbol(underlyingType, elementNames, elementLocations, elementNullableAnnotations: default);
        }

        protected abstract INamedTypeSymbol CommonCreateTupleTypeSymbol(
            INamedTypeSymbol underlyingType,
            ImmutableArray<string?> elementNames,
            ImmutableArray<Location?> elementLocations,
            ImmutableArray<NullableAnnotation> elementNullableAnnotations);

        /// <summary>
        /// Returns a new anonymous type symbol with the given member types, names, source locations, and nullable annotations.
        /// Anonymous type members will be readonly by default.  Writable properties are
        /// supported in VB and can be created by passing in <see langword="false"/> in the
        /// appropriate locations in <paramref name="memberIsReadOnly"/>.
        /// </summary>
        public INamedTypeSymbol CreateAnonymousTypeSymbol(
            ImmutableArray<ITypeSymbol> memberTypes,
            ImmutableArray<string> memberNames,
            ImmutableArray<bool> memberIsReadOnly = default,
            ImmutableArray<Location> memberLocations = default,
            ImmutableArray<NullableAnnotation> memberNullableAnnotations = default)
        {
            if (memberTypes.IsDefault)
            {
                throw new ArgumentNullException(nameof(memberTypes));
            }

            if (memberNames.IsDefault)
            {
                throw new ArgumentNullException(nameof(memberNames));
            }

            if (memberTypes.Length != memberNames.Length)
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.AnonymousTypeMemberAndNamesCountMismatch2,
                                                    nameof(memberTypes), nameof(memberNames)));
            }

            if (!memberLocations.IsDefault && memberLocations.Length != memberTypes.Length)
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.AnonymousTypeArgumentCountMismatch2,
                                                    nameof(memberLocations), nameof(memberNames)));
            }

            if (!memberIsReadOnly.IsDefault && memberIsReadOnly.Length != memberTypes.Length)
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.AnonymousTypeArgumentCountMismatch2,
                                                    nameof(memberIsReadOnly), nameof(memberNames)));
            }

            if (!memberNullableAnnotations.IsDefault && memberNullableAnnotations.Length != memberTypes.Length)
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.AnonymousTypeArgumentCountMismatch2,
                                                    nameof(memberNullableAnnotations), nameof(memberNames)));
            }

            for (int i = 0, n = memberTypes.Length; i < n; i++)
            {
                if (memberTypes[i] == null)
                {
                    throw new ArgumentNullException($"{nameof(memberTypes)}[{i}]");
                }

                if (memberNames[i] == null)
                {
                    throw new ArgumentNullException($"{nameof(memberNames)}[{i}]");
                }

                if (!memberLocations.IsDefault && memberLocations[i] == null)
                {
                    throw new ArgumentNullException($"{nameof(memberLocations)}[{i}]");
                }
            }

            return CommonCreateAnonymousTypeSymbol(memberTypes, memberNames, memberLocations, memberIsReadOnly, memberNullableAnnotations);
        }

        /// <summary>
        /// Returns a new anonymous type symbol with the given member types, names, and source locations.
        /// Anonymous type members will be readonly by default.  Writable properties are
        /// supported in VB and can be created by passing in <see langword="false"/> in the
        /// appropriate locations in <paramref name="memberIsReadOnly"/>.
        /// </summary>
        /// <remarks>This overload is for backwards compatibility. Do not remove.</remarks>
        public INamedTypeSymbol CreateAnonymousTypeSymbol(
            ImmutableArray<ITypeSymbol> memberTypes,
            ImmutableArray<string> memberNames,
            ImmutableArray<bool> memberIsReadOnly,
            ImmutableArray<Location> memberLocations)
        {
            return CreateAnonymousTypeSymbol(memberTypes, memberNames, memberIsReadOnly, memberLocations, memberNullableAnnotations: default);
        }

        protected abstract INamedTypeSymbol CommonCreateAnonymousTypeSymbol(
            ImmutableArray<ITypeSymbol> memberTypes,
            ImmutableArray<string> memberNames,
            ImmutableArray<Location> memberLocations,
            ImmutableArray<bool> memberIsReadOnly,
            ImmutableArray<NullableAnnotation> memberNullableAnnotations);

        /// <summary>
        /// Classifies a conversion from <paramref name="source"/> to <paramref name="destination"/> according
        /// to this compilation's programming language.
        /// </summary>
        /// <param name="source">Source type of value to be converted</param>
        /// <param name="destination">Destination type of value to be converted</param>
        /// <returns>A <see cref="CommonConversion"/> that classifies the conversion from the
        /// <paramref name="source"/> type to the <paramref name="destination"/> type.</returns>
        public abstract CommonConversion ClassifyCommonConversion(ITypeSymbol source, ITypeSymbol destination);

        /// <summary>
        /// Returns true if there is an implicit (C#) or widening (VB) conversion from
        /// <paramref name="fromType"/> to <paramref name="toType"/>. Returns false if
        /// either <paramref name="fromType"/> or <paramref name="toType"/> is null, or
        /// if no such conversion exists.
        /// </summary>
        public bool HasImplicitConversion(ITypeSymbol? fromType, ITypeSymbol? toType)
            => fromType != null && toType != null && this.ClassifyCommonConversion(fromType, toType).IsImplicit;

        /// <summary>
        /// Checks if <paramref name="symbol"/> is accessible from within <paramref name="within"/>. An optional qualifier of type
        /// <paramref name="throughType"/> is used to resolve protected access for instance members. All symbols are
        /// required to be from this compilation or some assembly referenced (<see cref="References"/>) by this
        /// compilation. <paramref name="within"/> is required to be an <see cref="INamedTypeSymbol"/> or <see cref="IAssemblySymbol"/>.
        /// </summary>
        /// <remarks>
        /// <para>Submissions can reference symbols from previous submissions and their referenced assemblies, even
        /// though those references are missing from <see cref="References"/>.
        /// See https://github.com/dotnet/roslyn/issues/27356.
        /// This implementation works around that by permitting symbols from previous submissions as well.</para>
        /// <para>It is advised to avoid the use of this API within the compilers, as the compilers have additional
        /// requirements for access checking that are not satisfied by this implementation, including the
        /// avoidance of infinite recursion that could result from the use of the ISymbol APIs here, the detection
        /// of use-site diagnostics, and additional returned details (from the compiler's internal APIs) that are
        /// helpful for more precisely diagnosing reasons for accessibility failure.</para>
        /// </remarks>
        public bool IsSymbolAccessibleWithin(
            ISymbol symbol,
            ISymbol within,
            ITypeSymbol? throughType = null)
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (within is null)
            {
                throw new ArgumentNullException(nameof(within));
            }

            if (!(within is INamedTypeSymbol || within is IAssemblySymbol))
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.IsSymbolAccessibleBadWithin, nameof(within)), nameof(within));
            }

            checkInCompilationReferences(symbol, nameof(symbol));
            checkInCompilationReferences(within, nameof(within));
            if (throughType is object)
            {
                checkInCompilationReferences(throughType, nameof(throughType));
            }

            return IsSymbolAccessibleWithinCore(symbol, within, throughType);

            void checkInCompilationReferences(ISymbol s, string parameterName)
            {
                if (!isContainingAssemblyInReferences(s))
                {
                    throw new ArgumentException(string.Format(CodeAnalysisResources.IsSymbolAccessibleWrongAssembly, parameterName), parameterName);
                }
            }

            bool assemblyIsInReferences(IAssemblySymbol a)
            {
                if (assemblyIsInCompilationReferences(a, this))
                {
                    return true;
                }

                if (this.IsSubmission)
                {
                    // Submissions can reference symbols from previous submissions and their referenced assemblies, even
                    // though those references are missing from this.References. We work around that by digging in
                    // to find references of previous submissions. See https://github.com/dotnet/roslyn/issues/27356
                    for (Compilation? c = this.PreviousSubmission; c != null; c = c.PreviousSubmission)
                    {
                        if (assemblyIsInCompilationReferences(a, c))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            bool assemblyIsInCompilationReferences(IAssemblySymbol a, Compilation compilation)
            {
                if (a.Equals(compilation.Assembly))
                {
                    return true;
                }

                foreach (var reference in compilation.References)
                {
                    if (a.Equals(compilation.GetAssemblyOrModuleSymbol(reference)))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool isContainingAssemblyInReferences(ISymbol s)
            {
                while (true)
                {
                    switch (s.Kind)
                    {
                        case SymbolKind.Assembly:
                            return assemblyIsInReferences((IAssemblySymbol)s);
                        case SymbolKind.PointerType:
                            s = ((IPointerTypeSymbol)s).PointedAtType;
                            continue;
                        case SymbolKind.ArrayType:
                            s = ((IArrayTypeSymbol)s).ElementType;
                            continue;
                        case SymbolKind.Alias:
                            s = ((IAliasSymbol)s).Target;
                            continue;
                        case SymbolKind.Discard:
                            s = ((IDiscardSymbol)s).Type;
                            continue;
                        case SymbolKind.FunctionPointer:
                            var funcPtr = (IFunctionPointerTypeSymbol)s;
                            if (!isContainingAssemblyInReferences(funcPtr.Signature.ReturnType))
                            {
                                return false;
                            }

                            foreach (var param in funcPtr.Signature.Parameters)
                            {
                                if (!isContainingAssemblyInReferences(param.Type))
                                {
                                    return false;
                                }
                            }

                            return true;
                        case SymbolKind.DynamicType:
                        case SymbolKind.ErrorType:
                        case SymbolKind.Preprocessing:
                        case SymbolKind.Namespace:
                            // these symbols are not restricted in where they can be accessed, so unless they report
                            // a containing assembly, we treat them as in the current assembly for access purposes
                            return assemblyIsInReferences(s.ContainingAssembly ?? this.Assembly);
                        default:
                            return assemblyIsInReferences(s.ContainingAssembly);
                    }
                }
            }
        }

        private protected abstract bool IsSymbolAccessibleWithinCore(
            ISymbol symbol,
            ISymbol within,
            ITypeSymbol? throughType);

        internal abstract IConvertibleConversion ClassifyConvertibleConversion(IOperation source, ITypeSymbol destination, out Optional<object> constantValue);

        #endregion

        #region Diagnostics

        internal const CompilationStage DefaultDiagnosticsStage = CompilationStage.Compile;

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

        internal abstract void GetDiagnostics(CompilationStage stage, bool includeEarlierStages, DiagnosticBag diagnostics, CancellationToken cancellationToken = default);

        internal void EnsureCompilationEventQueueCompleted()
        {
            RoslynDebug.Assert(EventQueue != null);

            lock (EventQueue)
            {
                if (!EventQueue.IsCompleted)
                {
                    CompleteCompilationEventQueue_NoLock();
                }
            }
        }

        internal void CompleteCompilationEventQueue_NoLock()
        {
            RoslynDebug.Assert(EventQueue != null);

            // Signal the end of compilation.
            EventQueue.TryEnqueue(new CompilationCompletedEvent(this));
            EventQueue.PromiseNotToEnqueue();
            EventQueue.TryComplete();
        }

        internal abstract CommonMessageProvider MessageProvider { get; }

        /// <summary>
        /// Filter out warnings based on the compiler options (/nowarn, /warn and /warnaserror) and the pragma warning directives.
        /// 'incoming' is freed.
        /// </summary>
        /// <param name="accumulator">Bag to which filtered diagnostics will be added.</param>
        /// <param name="incoming">Diagnostics to be filtered.</param>
        /// <returns>True if there are no unsuppressed errors (i.e., no errors which fail compilation).</returns>
        internal bool FilterAndAppendAndFreeDiagnostics(DiagnosticBag accumulator, [DisallowNull] ref DiagnosticBag? incoming)
        {
            RoslynDebug.Assert(incoming is object);
            bool result = FilterAndAppendDiagnostics(accumulator, incoming.AsEnumerableWithoutResolution(), exclude: null);
            incoming.Free();
            incoming = null;
            return result;
        }

        /// <summary>
        /// Filter out warnings based on the compiler options (/nowarn, /warn and /warnaserror) and the pragma warning directives.
        /// </summary>
        /// <returns>True if there are no unsuppressed errors (i.e., no errors which fail compilation).</returns>
        internal bool FilterAndAppendDiagnostics(DiagnosticBag accumulator, IEnumerable<Diagnostic> incoming, HashSet<int>? exclude)
        {
            bool hasError = false;
            bool reportSuppressedDiagnostics = Options.ReportSuppressedDiagnostics;

            foreach (Diagnostic d in incoming)
            {
                if (exclude?.Contains(d.Code) == true)
                {
                    continue;
                }

                var filtered = Options.FilterDiagnostic(d);
                if (filtered == null ||
                    (!reportSuppressedDiagnostics && filtered.IsSuppressed))
                {
                    continue;
                }
                else if (filtered.IsUnsuppressableError())
                {
                    hasError = true;
                }

                accumulator.Add(filtered);
            }

            return !hasError;
        }

        #endregion

        #region Resources

        /// <summary>
        /// Create a stream filled with default win32 resources.
        /// </summary>
        public Stream CreateDefaultWin32Resources(bool versionResource, bool noManifest, Stream? manifestContents, Stream? iconInIcoFormat)
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

        internal static Win32ResourceForm DetectWin32ResourceForm(Stream win32Resources)
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

        internal Cci.ResourceSection? MakeWin32ResourcesFromCOFF(Stream? win32Resources, DiagnosticBag diagnostics)
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

        internal List<Win32Resource>? MakeWin32ResourceList(Stream? win32Resources, DiagnosticBag diagnostics)
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
                    id: unchecked((short)r.pstringName!.Ordinal),
                    name: r.pstringName.theString,
                    typeId: unchecked((short)r.pstringType!.Ordinal),
                    typeName: r.pstringType.theString
                );

                resourceList.Add(result);
            }

            return resourceList;
        }

        internal void SetupWin32Resources(CommonPEModuleBuilder moduleBeingBuilt, Stream? win32Resources, DiagnosticBag diagnostics)
        {
            if (win32Resources == null)
                return;

            Win32ResourceForm resourceForm;

            try
            {
                resourceForm = DetectWin32ResourceForm(win32Resources);
            }
            catch (EndOfStreamException)
            {
                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_BadWin32Resource, NoLocation.Singleton, CodeAnalysisResources.UnrecognizedResourceFileFormat));
                return;
            }
            catch (Exception ex)
            {
                diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_BadWin32Resource, NoLocation.Singleton, ex.Message));
                return;
            }

            switch (resourceForm)
            {
                case Win32ResourceForm.COFF:
                    moduleBeingBuilt.Win32ResourceSection = MakeWin32ResourcesFromCOFF(win32Resources, diagnostics);
                    break;
                case Win32ResourceForm.RES:
                    moduleBeingBuilt.Win32Resources = MakeWin32ResourceList(win32Resources, diagnostics);
                    break;
                default:
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_BadWin32Resource, NoLocation.Singleton, CodeAnalysisResources.UnrecognizedResourceFileFormat));
                    break;
            }
        }

        internal void ReportManifestResourceDuplicates(
            IEnumerable<ResourceDescription>? manifestResources,
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
        /// There are two ways to sign PE files
        ///   1. By directly signing the <see cref="PEBuilder"/>
        ///   2. Write the unsigned PE to disk and use CLR COM APIs to sign.
        /// The preferred method is #1 as it's more efficient and more resilient (no reliance on %TEMP%). But 
        /// we must continue to support #2 as it's the only way to do the following:
        ///   - Access private keys stored in a key container
        ///   - Do proper counter signature verification for AssemblySignatureKey attributes
        /// </summary>
        internal bool SignUsingBuilder =>
            string.IsNullOrEmpty(StrongNameKeys.KeyContainer) &&
            !StrongNameKeys.HasCounterSignature &&
            !_features.ContainsKey("UseLegacyStrongNameProvider");

        /// <summary>
        /// Constructs the module serialization properties out of the compilation options of this compilation.
        /// </summary>
        internal Cci.ModulePropertiesForSerialization ConstructModuleSerializationProperties(
            EmitOptions emitOptions,
            string? targetRuntimeVersion,
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
                case Platform.Arm64:
                    machine = Machine.Arm64;
                    break;

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
                corFlags: GetCorHeaderFlags(machine, HasStrongName, prefers32Bit: platform == Platform.AnyCpu32BitPreferred),
                fileAlignment: fileAlignment,
                sectionAlignment: Cci.ModulePropertiesForSerialization.DefaultSectionAlignment,
                targetRuntimeVersion: targetRuntimeVersion,
                machine: machine,
                baseAddress: baseAddress,
                sizeOfHeapReserve: sizeOfHeapReserve,
                sizeOfHeapCommit: sizeOfHeapCommit,
                sizeOfStackReserve: sizeOfStackReserve,
                sizeOfStackCommit: sizeOfStackCommit,
                dllCharacteristics: GetDllCharacteristics(emitOptions.HighEntropyVirtualAddressSpace, compilationOptions.OutputKind == OutputKind.WindowsRuntimeApplication),
                imageCharacteristics: GetCharacteristics(outputKind, requires32Bit),
                subsystem: GetSubsystem(outputKind),
                majorSubsystemVersion: (ushort)subsystemVersion.Major,
                minorSubsystemVersion: (ushort)subsystemVersion.Minor,
                linkerMajorVersion: this.LinkerMajorVersion,
                linkerMinorVersion: 0);
        }

        private static CorFlags GetCorHeaderFlags(Machine machine, bool strongNameSigned, bool prefers32Bit)
        {
            CorFlags result = CorFlags.ILOnly;

            if (machine == Machine.I386)
            {
                result |= CorFlags.Requires32Bit;
            }

            if (strongNameSigned)
            {
                result |= CorFlags.StrongNameSigned;
            }

            if (prefers32Bit)
            {
                result |= CorFlags.Requires32Bit | CorFlags.Prefers32Bit;
            }

            return result;
        }

        internal static DllCharacteristics GetDllCharacteristics(bool enableHighEntropyVA, bool configureToExecuteInAppContainer)
        {
            var result =
                DllCharacteristics.DynamicBase |
                DllCharacteristics.NxCompatible |
                DllCharacteristics.NoSeh |
                DllCharacteristics.TerminalServerAware;

            if (enableHighEntropyVA)
            {
                // IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA
                result |= (DllCharacteristics)0x0020;
            }

            if (configureToExecuteInAppContainer)
            {
                result |= DllCharacteristics.AppContainer;
            }

            return result;
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

        internal abstract CommonPEModuleBuilder? CreateModuleBuilder(
            EmitOptions emitOptions,
            IMethodSymbol? debugEntryPoint,
            Stream? sourceLinkStream,
            IEnumerable<EmbeddedText>? embeddedTexts,
            IEnumerable<ResourceDescription>? manifestResources,
            CompilationTestData? testData,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken);

        /// <summary>
        /// Report declaration diagnostics and compile and synthesize method bodies.
        /// </summary>
        /// <returns>True if successful.</returns>
        internal abstract bool CompileMethods(
            CommonPEModuleBuilder moduleBuilder,
            bool emittingPdb,
            bool emitMetadataOnly,
            bool emitTestCoverageData,
            DiagnosticBag diagnostics,
            Predicate<ISymbolInternal>? filterOpt,
            CancellationToken cancellationToken);

        internal bool CreateDebugDocuments(DebugDocumentsBuilder documentsBuilder, IEnumerable<EmbeddedText> embeddedTexts, DiagnosticBag diagnostics)
        {
            // Check that all syntax trees are debuggable:
            bool allTreesDebuggable = true;
            foreach (var tree in SyntaxTrees)
            {
                if (!string.IsNullOrEmpty(tree.FilePath) && tree.GetText().Encoding == null)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_EncodinglessSyntaxTree, tree.GetRoot().GetLocation()));
                    allTreesDebuggable = false;
                }
            }

            if (!allTreesDebuggable)
            {
                return false;
            }

            // Add debug documents for all embedded text first. This ensures that embedding
            // takes priority over the syntax tree pass, which will not embed.
            if (!embeddedTexts.IsEmpty())
            {
                foreach (var text in embeddedTexts)
                {
                    Debug.Assert(!string.IsNullOrEmpty(text.FilePath));
                    string normalizedPath = documentsBuilder.NormalizeDebugDocumentPath(text.FilePath, basePath: null);
                    var existingDoc = documentsBuilder.TryGetDebugDocumentForNormalizedPath(normalizedPath);
                    if (existingDoc == null)
                    {
                        var document = new Cci.DebugSourceDocument(
                            normalizedPath,
                            DebugSourceDocumentLanguageId,
                            () => text.GetDebugSourceInfo());

                        documentsBuilder.AddDebugDocument(document);
                    }
                }
            }

            // Add debug documents for all trees with distinct paths.
            foreach (var tree in SyntaxTrees)
            {
                if (!string.IsNullOrEmpty(tree.FilePath))
                {
                    // compilation does not guarantee that all trees will have distinct paths.
                    // Do not attempt adding a document for a particular path if we already added one.
                    string normalizedPath = documentsBuilder.NormalizeDebugDocumentPath(tree.FilePath, basePath: null);
                    var existingDoc = documentsBuilder.TryGetDebugDocumentForNormalizedPath(normalizedPath);
                    if (existingDoc == null)
                    {
                        documentsBuilder.AddDebugDocument(new Cci.DebugSourceDocument(
                            normalizedPath,
                            DebugSourceDocumentLanguageId,
                            () => tree.GetDebugSourceInfo()));
                    }
                }
            }

            // Add debug documents for all pragmas.
            // If there are clashes with already processed directives, report warnings.
            // If there are clashes with debug documents that came from actual trees, ignore the pragma.
            // Therefore we need to add these in a separate pass after documents for syntax trees were added.
            foreach (var tree in SyntaxTrees)
            {
                AddDebugSourceDocumentsForChecksumDirectives(documentsBuilder, tree, diagnostics);
            }

            return true;
        }

        internal abstract Guid DebugSourceDocumentLanguageId { get; }

        internal abstract void AddDebugSourceDocumentsForChecksumDirectives(DebugDocumentsBuilder documentsBuilder, SyntaxTree tree, DiagnosticBag diagnostics);

        /// <summary>
        /// Update resources and generate XML documentation comments.
        /// </summary>
        /// <returns>True if successful.</returns>
        internal abstract bool GenerateResourcesAndDocumentationComments(
            CommonPEModuleBuilder moduleBeingBuilt,
            Stream? xmlDocumentationStream,
            Stream? win32ResourcesStream,
            string? outputNameOverride,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken);

        /// <summary>
        /// Reports all unused imports/usings so far (and thus it must be called as a last step of Emit)
        /// </summary>
        internal abstract void ReportUnusedImports(
            SyntaxTree? filterTree,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken);

        /// <summary>
        /// Signals the event queue, if any, that we are done compiling.
        /// There should not be more compiling actions after this step.
        /// NOTE: once we signal about completion to analyzers they will cancel and thus in some cases we
        ///       may be effectively cutting off some diagnostics.
        ///       It is not clear if behavior is desirable.
        ///       See: https://github.com/dotnet/roslyn/issues/11470
        /// </summary>
        /// <param name="filterTree">What tree to complete. null means complete all trees. </param>
        internal abstract void CompleteTrees(SyntaxTree? filterTree);

        internal bool Compile(
            CommonPEModuleBuilder moduleBuilder,
            bool emittingPdb,
            DiagnosticBag diagnostics,
            Predicate<ISymbolInternal>? filterOpt,
            CancellationToken cancellationToken)
        {
            try
            {
                return CompileMethods(
                    moduleBuilder,
                    emittingPdb,
                    emitMetadataOnly: false,
                    emitTestCoverageData: false,
                    diagnostics: diagnostics,
                    filterOpt: filterOpt,
                    cancellationToken: cancellationToken);
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
                        sourceLinkStream: null,
                        embeddedTexts: null,
                        testData: null,
                        diagnostics: discardedDiagnostics,
                        cancellationToken: cancellationToken);

                    if (moduleBeingBuilt != null)
                    {
                        Compile(
                            moduleBeingBuilt,
                            diagnostics: discardedDiagnostics,
                            emittingPdb: false,
                            filterOpt: null,
                            cancellationToken: cancellationToken);
                    }

                    discardedDiagnostics.Free();
                }

                Debug.Assert(this.CommonAnonymousTypeManager.AreTemplatesSealed);
            }
            else
            {
                this.ScriptCompilationInfo?.PreviousScriptCompilation?.EnsureAnonymousTypeTemplates(cancellationToken);
            }
        }

        // 1.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EmitResult Emit(
            Stream peStream,
            Stream? pdbStream,
            Stream? xmlDocumentationStream,
            Stream? win32Resources,
            IEnumerable<ResourceDescription>? manifestResources,
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
                debugEntryPoint: null,
                sourceLinkStream: null,
                embeddedTexts: null,
                cancellationToken);
        }

        // 1.3 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EmitResult Emit(
            Stream peStream,
            Stream pdbStream,
            Stream xmlDocumentationStream,
            Stream win32Resources,
            IEnumerable<ResourceDescription> manifestResources,
            EmitOptions options,
            IMethodSymbol debugEntryPoint,
            CancellationToken cancellationToken)
        {
            return Emit(
                peStream,
                pdbStream,
                xmlDocumentationStream,
                win32Resources,
                manifestResources,
                options,
                debugEntryPoint,
                sourceLinkStream: null,
                embeddedTexts: null,
                cancellationToken);
        }

        // 2.0 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        public EmitResult Emit(
            Stream peStream,
            Stream? pdbStream,
            Stream? xmlDocumentationStream,
            Stream? win32Resources,
            IEnumerable<ResourceDescription>? manifestResources,
            EmitOptions options,
            IMethodSymbol? debugEntryPoint,
            Stream? sourceLinkStream,
            IEnumerable<EmbeddedText>? embeddedTexts,
            CancellationToken cancellationToken)
        {
            return Emit(
                peStream,
                pdbStream,
                xmlDocumentationStream,
                win32Resources,
                manifestResources,
                options,
                debugEntryPoint,
                sourceLinkStream,
                embeddedTexts,
                metadataPEStream: null,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Emit the IL for the compiled source code into the specified stream.
        /// </summary>
        /// <param name="peStream">Stream to which the compilation will be written.</param>
        /// <param name="metadataPEStream">Stream to which the metadata-only output will be written.</param>
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
        /// <param name="sourceLinkStream">
        /// Stream containing information linking the compilation to a source control.
        /// </param>
        /// <param name="embeddedTexts">
        /// Texts to embed in the PDB.
        /// Only supported when emitting Portable PDBs.
        /// </param>
        /// <param name="cancellationToken">To cancel the emit process.</param>
        public EmitResult Emit(
            Stream peStream,
            Stream? pdbStream = null,
            Stream? xmlDocumentationStream = null,
            Stream? win32Resources = null,
            IEnumerable<ResourceDescription>? manifestResources = null,
            EmitOptions? options = null,
            IMethodSymbol? debugEntryPoint = null,
            Stream? sourceLinkStream = null,
            IEnumerable<EmbeddedText>? embeddedTexts = null,
            Stream? metadataPEStream = null,
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

            if (pdbStream != null)
            {
                if (options?.DebugInformationFormat == DebugInformationFormat.Embedded)
                {
                    throw new ArgumentException(CodeAnalysisResources.PdbStreamUnexpectedWhenEmbedding, nameof(pdbStream));
                }

                if (!pdbStream.CanWrite)
                {
                    throw new ArgumentException(CodeAnalysisResources.StreamMustSupportWrite, nameof(pdbStream));
                }

                if (options?.EmitMetadataOnly == true)
                {
                    throw new ArgumentException(CodeAnalysisResources.PdbStreamUnexpectedWhenEmittingMetadataOnly, nameof(pdbStream));
                }
            }

            if (metadataPEStream != null && options?.EmitMetadataOnly == true)
            {
                throw new ArgumentException(CodeAnalysisResources.MetadataPeStreamUnexpectedWhenEmittingMetadataOnly, nameof(metadataPEStream));
            }

            if (metadataPEStream != null && options?.IncludePrivateMembers == true)
            {
                throw new ArgumentException(CodeAnalysisResources.IncludingPrivateMembersUnexpectedWhenEmittingToMetadataPeStream, nameof(metadataPEStream));
            }

            if (metadataPEStream == null && options?.EmitMetadataOnly == false)
            {
                // EmitOptions used to default to IncludePrivateMembers=false, so to preserve binary compatibility we silently correct that unless emitting regular assemblies
                options = options.WithIncludePrivateMembers(true);
            }

            if (options?.DebugInformationFormat == DebugInformationFormat.Embedded &&
                options?.EmitMetadataOnly == true)
            {
                throw new ArgumentException(CodeAnalysisResources.EmbeddingPdbUnexpectedWhenEmittingMetadata, nameof(metadataPEStream));
            }

            if (this.Options.OutputKind == OutputKind.NetModule)
            {
                if (metadataPEStream != null)
                {
                    throw new ArgumentException(CodeAnalysisResources.CannotTargetNetModuleWhenEmittingRefAssembly, nameof(metadataPEStream));
                }
                else if (options?.EmitMetadataOnly == true)
                {
                    throw new ArgumentException(CodeAnalysisResources.CannotTargetNetModuleWhenEmittingRefAssembly, nameof(options.EmitMetadataOnly));
                }
            }

            if (win32Resources != null)
            {
                if (!win32Resources.CanRead || !win32Resources.CanSeek)
                {
                    throw new ArgumentException(CodeAnalysisResources.StreamMustSupportReadAndSeek, nameof(win32Resources));
                }
            }

            if (sourceLinkStream != null && !sourceLinkStream.CanRead)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportRead, nameof(sourceLinkStream));
            }

            if (embeddedTexts != null &&
                !embeddedTexts.IsEmpty() &&
                pdbStream == null &&
                options?.DebugInformationFormat != DebugInformationFormat.Embedded)
            {
                throw new ArgumentException(CodeAnalysisResources.EmbeddedTextsRequirePdb, nameof(embeddedTexts));
            }

            return Emit(
                peStream,
                metadataPEStream,
                pdbStream,
                xmlDocumentationStream,
                win32Resources,
                manifestResources,
                options,
                debugEntryPoint,
                sourceLinkStream,
                embeddedTexts,
                testData: null,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// This overload is only intended to be directly called by tests that want to pass <paramref name="testData"/>.
        /// The map is used for storing a list of methods and their associated IL.
        /// </summary>
        internal EmitResult Emit(
            Stream peStream,
            Stream? metadataPEStream,
            Stream? pdbStream,
            Stream? xmlDocumentationStream,
            Stream? win32Resources,
            IEnumerable<ResourceDescription>? manifestResources,
            EmitOptions? options,
            IMethodSymbol? debugEntryPoint,
            Stream? sourceLinkStream,
            IEnumerable<EmbeddedText>? embeddedTexts,
            CompilationTestData? testData,
            CancellationToken cancellationToken)
        {
            options = options ?? EmitOptions.Default.WithIncludePrivateMembers(metadataPEStream == null);

            bool embedPdb = options.DebugInformationFormat == DebugInformationFormat.Embedded;
            Debug.Assert(!embedPdb || pdbStream == null);
            Debug.Assert(metadataPEStream == null || !options.IncludePrivateMembers); // you may not use a secondary stream and include private members together

            var diagnostics = DiagnosticBag.GetInstance();

            var moduleBeingBuilt = CheckOptionsAndCreateModuleBuilder(
                diagnostics,
                manifestResources,
                options,
                debugEntryPoint,
                sourceLinkStream,
                embeddedTexts,
                testData,
                cancellationToken);

            bool success = false;

            if (moduleBeingBuilt != null)
            {
                try
                {
                    success = CompileMethods(
                        moduleBeingBuilt,
                        emittingPdb: pdbStream != null || embedPdb,
                        emitMetadataOnly: options.EmitMetadataOnly,
                        emitTestCoverageData: options.EmitTestCoverageData,
                        diagnostics: diagnostics,
                        filterOpt: null,
                        cancellationToken: cancellationToken);

                    if (!options.EmitMetadataOnly)
                    {
                        // NOTE: We generate documentation even in presence of compile errors.
                        // https://github.com/dotnet/roslyn/issues/37996 tracks revisiting this behavior.
                        if (!GenerateResourcesAndDocumentationComments(
                            moduleBeingBuilt,
                            xmlDocumentationStream,
                            win32Resources,
                            options.OutputNameOverride,
                            diagnostics,
                            cancellationToken))
                        {
                            success = false;
                        }

                        if (success)
                        {
                            ReportUnusedImports(null, diagnostics, cancellationToken);
                        }
                    }
                }
                finally
                {
                    moduleBeingBuilt.CompilationFinished();
                }

                RSAParameters? privateKeyOpt = null;
                if (Options.StrongNameProvider != null && SignUsingBuilder && !Options.PublicSign)
                {
                    privateKeyOpt = StrongNameKeys.PrivateKey;
                }

                if (!options.EmitMetadataOnly && CommonCompiler.HasUnsuppressedErrors(diagnostics))
                {
                    success = false;
                }

                if (success)
                {
                    success = SerializeToPeStream(
                        moduleBeingBuilt,
                        new SimpleEmitStreamProvider(peStream),
                        (metadataPEStream != null) ? new SimpleEmitStreamProvider(metadataPEStream) : null,
                        (pdbStream != null) ? new SimpleEmitStreamProvider(pdbStream) : null,
                        testData?.SymWriterFactory,
                        diagnostics,
                        emitOptions: options,
                        privateKeyOpt: privateKeyOpt,
                        cancellationToken: cancellationToken);
                }
            }

            return new EmitResult(success, diagnostics.ToReadOnlyAndFree());
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

            return this.EmitDifference(baseline, edits, isAddedSymbol, metadataStream, ilStream, pdbStream, updatedMethods, testData: null, cancellationToken);
        }

        internal abstract EmitDifferenceResult EmitDifference(
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Func<ISymbol, bool> isAddedSymbol,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<MethodDefinitionHandle> updatedMethodHandles,
            CompilationTestData? testData,
            CancellationToken cancellationToken);

        /// <summary>
        /// Check compilation options and create <see cref="CommonPEModuleBuilder"/>.
        /// </summary>
        /// <returns><see cref="CommonPEModuleBuilder"/> if successful.</returns>
        internal CommonPEModuleBuilder? CheckOptionsAndCreateModuleBuilder(
            DiagnosticBag diagnostics,
            IEnumerable<ResourceDescription>? manifestResources,
            EmitOptions options,
            IMethodSymbol? debugEntryPoint,
            Stream? sourceLinkStream,
            IEnumerable<EmbeddedText>? embeddedTexts,
            CompilationTestData? testData,
            CancellationToken cancellationToken)
        {
            options.ValidateOptions(diagnostics, MessageProvider, Options.Deterministic);

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

            if (CommonCompiler.HasUnsuppressableErrors(diagnostics))
            {
                return null;
            }

            // Do not waste a slot in the submission chain for submissions that contain no executable code
            // (they may only contain #r directives, usings, etc.)
            if (IsSubmission && !HasCodeToEmit())
            {
                // Still report diagnostics since downstream submissions will assume there are no errors.
                diagnostics.AddRange(this.GetDiagnostics(cancellationToken));
                return null;
            }

            return this.CreateModuleBuilder(
                options,
                debugEntryPoint,
                sourceLinkStream,
                embeddedTexts,
                manifestResources,
                testData,
                diagnostics,
                cancellationToken);
        }

        internal abstract void ValidateDebugEntryPoint(IMethodSymbol debugEntryPoint, DiagnosticBag diagnostics);

        internal bool IsEmitDeterministic => this.Options.Deterministic;

        internal bool SerializeToPeStream(
            CommonPEModuleBuilder moduleBeingBuilt,
            EmitStreamProvider peStreamProvider,
            EmitStreamProvider? metadataPEStreamProvider,
            EmitStreamProvider? pdbStreamProvider,
            Func<ISymWriterMetadataProvider, SymUnmanagedWriter>? testSymWriterFactory,
            DiagnosticBag diagnostics,
            EmitOptions emitOptions,
            RSAParameters? privateKeyOpt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Cci.PdbWriter? nativePdbWriter = null;
            DiagnosticBag? metadataDiagnostics = null;
            DiagnosticBag? pdbBag = null;

            bool deterministic = IsEmitDeterministic;

            // PDB Stream provider should not be given if PDB is to be embedded into the PE file:
            Debug.Assert(moduleBeingBuilt.DebugInformationFormat != DebugInformationFormat.Embedded || pdbStreamProvider == null);

            string? pePdbFilePath = emitOptions.PdbFilePath;

            if (moduleBeingBuilt.DebugInformationFormat == DebugInformationFormat.Embedded || pdbStreamProvider != null)
            {
                pePdbFilePath = pePdbFilePath ?? FileNameUtilities.ChangeExtension(SourceModule.Name, "pdb");
            }
            else
            {
                pePdbFilePath = null;
            }

            if (moduleBeingBuilt.DebugInformationFormat == DebugInformationFormat.Embedded && !RoslynString.IsNullOrEmpty(pePdbFilePath))
            {
                pePdbFilePath = PathUtilities.GetFileName(pePdbFilePath);
            }

            EmitStream? emitPeStream = null;
            EmitStream? emitMetadataStream = null;
            try
            {
                var signKind = IsRealSigned
                    ? (SignUsingBuilder ? EmitStreamSignKind.SignedWithBuilder : EmitStreamSignKind.SignedWithFile)
                    : EmitStreamSignKind.None;
                emitPeStream = new EmitStream(peStreamProvider, signKind, Options.StrongNameProvider);
                emitMetadataStream = metadataPEStreamProvider == null
                    ? null
                    : new EmitStream(metadataPEStreamProvider, signKind, Options.StrongNameProvider);
                metadataDiagnostics = DiagnosticBag.GetInstance();

                if (moduleBeingBuilt.DebugInformationFormat == DebugInformationFormat.Pdb && pdbStreamProvider != null)
                {
                    // The algorithm must be specified for deterministic builds (checked earlier).
                    Debug.Assert(!deterministic || moduleBeingBuilt.PdbChecksumAlgorithm.Name != null);

                    // The calls ISymUnmanagedWriter2.GetDebugInfo require a file name in order to succeed.  This is
                    // frequently used during PDB writing.  Ensure a name is provided here in the case we were given
                    // only a Stream value.
                    nativePdbWriter = new Cci.PdbWriter(pePdbFilePath, testSymWriterFactory, deterministic ? moduleBeingBuilt.PdbChecksumAlgorithm : default);
                }

                Func<Stream?>? getPortablePdbStream =
                    moduleBeingBuilt.DebugInformationFormat != DebugInformationFormat.PortablePdb || pdbStreamProvider == null
                    ? null
                    : (Func<Stream?>)(() => ConditionalGetOrCreateStream(pdbStreamProvider, metadataDiagnostics));

                try
                {
                    if (SerializePeToStream(
                        moduleBeingBuilt,
                        metadataDiagnostics,
                        MessageProvider,
                        emitPeStream.GetCreateStreamFunc(metadataDiagnostics),
                        emitMetadataStream?.GetCreateStreamFunc(metadataDiagnostics),
                        getPortablePdbStream,
                        nativePdbWriter,
                        pePdbFilePath,
                        emitOptions.EmitMetadataOnly,
                        emitOptions.IncludePrivateMembers,
                        deterministic,
                        emitOptions.EmitTestCoverageData,
                        privateKeyOpt,
                        cancellationToken))
                    {
                        if (nativePdbWriter != null)
                        {
                            var nativePdbStream = pdbStreamProvider!.GetOrCreateStream(metadataDiagnostics);
                            Debug.Assert(nativePdbStream != null || metadataDiagnostics.HasAnyErrors());

                            if (nativePdbStream != null)
                            {
                                nativePdbWriter.WriteTo(nativePdbStream);
                            }
                        }
                    }
                }
                catch (SymUnmanagedWriterException ex)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PdbWritingFailed, Location.None, ex.Message));
                    return false;
                }
                catch (Cci.PeWritingException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PeWritingFailure, Location.None, e.InnerException?.ToString() ?? ""));
                    return false;
                }
                catch (ResourceException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_CantReadResource, Location.None, e.Message, e.InnerException?.Message ?? ""));
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

                return
                    emitPeStream.Complete(StrongNameKeys, MessageProvider, diagnostics) &&
                    (emitMetadataStream?.Complete(StrongNameKeys, MessageProvider, diagnostics) ?? true);
            }
            finally
            {
                nativePdbWriter?.Dispose();
                emitPeStream?.Close();
                emitMetadataStream?.Close();
                pdbBag?.Free();
                metadataDiagnostics?.Free();
            }
        }

        private static Stream? ConditionalGetOrCreateStream(EmitStreamProvider metadataPEStreamProvider, DiagnosticBag metadataDiagnostics)
        {
            if (metadataDiagnostics.HasAnyErrors())
            {
                return null;
            }

            var auxStream = metadataPEStreamProvider.GetOrCreateStream(metadataDiagnostics);
            Debug.Assert(auxStream != null || metadataDiagnostics.HasAnyErrors());
            return auxStream;
        }

        internal static bool SerializePeToStream(
            CommonPEModuleBuilder moduleBeingBuilt,
            DiagnosticBag metadataDiagnostics,
            CommonMessageProvider messageProvider,
            Func<Stream?> getPeStream,
            Func<Stream?>? getMetadataPeStreamOpt,
            Func<Stream?>? getPortablePdbStreamOpt,
            Cci.PdbWriter? nativePdbWriterOpt,
            string? pdbPathOpt,
            bool metadataOnly,
            bool includePrivateMembers,
            bool isDeterministic,
            bool emitTestCoverageData,
            RSAParameters? privateKeyOpt,
            CancellationToken cancellationToken)
        {
            bool emitSecondaryAssembly = getMetadataPeStreamOpt != null;

            bool includePrivateMembersOnPrimaryOutput = metadataOnly ? includePrivateMembers : true;
            bool deterministicPrimaryOutput = (metadataOnly && !includePrivateMembers) || isDeterministic;
            if (!Cci.PeWriter.WritePeToStream(
                new EmitContext(moduleBeingBuilt, null, metadataDiagnostics, metadataOnly, includePrivateMembersOnPrimaryOutput),
                messageProvider,
                getPeStream,
                getPortablePdbStreamOpt,
                nativePdbWriterOpt,
                pdbPathOpt,
                metadataOnly,
                deterministicPrimaryOutput,
                emitTestCoverageData,
                privateKeyOpt,
                cancellationToken))
            {
                return false;
            }

            // produce the secondary output (ref assembly) if needed
            if (emitSecondaryAssembly)
            {
                Debug.Assert(!metadataOnly);
                Debug.Assert(!includePrivateMembers);

                if (!Cci.PeWriter.WritePeToStream(
                    new EmitContext(moduleBeingBuilt, null, metadataDiagnostics, metadataOnly: true, includePrivateMembers: false),
                    messageProvider,
                    getMetadataPeStreamOpt,
                    getPortablePdbStreamOpt: null,
                    nativePdbWriterOpt: null,
                    pdbPathOpt: null,
                    metadataOnly: true,
                    isDeterministic: true,
                    emitTestCoverageData: false,
                    privateKeyOpt: privateKeyOpt,
                    cancellationToken: cancellationToken))
                {
                    return false;
                }
            }

            return true;
        }

        internal EmitBaseline? SerializeToDeltaStreams(
            CommonPEModuleBuilder moduleBeingBuilt,
            EmitBaseline baseline,
            DefinitionMap definitionMap,
            SymbolChanges changes,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<MethodDefinitionHandle> updatedMethods,
            DiagnosticBag diagnostics,
            Func<ISymWriterMetadataProvider, SymUnmanagedWriter> testSymWriterFactory,
            string pdbFilePath,
            CancellationToken cancellationToken)
        {
            var nativePdbWriterOpt = (moduleBeingBuilt.DebugInformationFormat != DebugInformationFormat.Pdb) ? null :
                new Cci.PdbWriter(
                    pdbFilePath ?? FileNameUtilities.ChangeExtension(SourceModule.Name, "pdb"),
                    testSymWriterFactory,
                    hashAlgorithmNameOpt: default);

            using (nativePdbWriterOpt)
            {
                var context = new EmitContext(moduleBeingBuilt, null, diagnostics, metadataOnly: false, includePrivateMembers: true);
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

                    writer.WriteMetadataAndIL(
                        nativePdbWriterOpt,
                        metadataStream,
                        ilStream,
                        (nativePdbWriterOpt == null) ? pdbStream : null,
                        out MetadataSizes metadataSizes);

                    writer.GetMethodTokens(updatedMethods);

                    nativePdbWriterOpt?.WriteTo(pdbStream);

                    return diagnostics.HasAnyErrors() ? null : writer.GetDelta(baseline, this, encId, metadataSizes);
                }
                catch (SymUnmanagedWriterException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PdbWritingFailed, Location.None, e.Message));
                    return null;
                }
                catch (Cci.PeWritingException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PeWritingFailure, Location.None, e.InnerException?.ToString() ?? ""));
                    return null;
                }
                catch (PermissionSetFileReadException e)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PermissionSetAttributeFileReadError, Location.None, e.FileName, e.PropertyName, e.Message));
                    return null;
                }
            }
        }

        internal string? Feature(string p)
        {
            string? v;
            return _features.TryGetValue(p, out v) ? v : null;
        }

        #endregion

        private ConcurrentDictionary<SyntaxTree, SmallConcurrentSetOfInts>? _lazyTreeToUsedImportDirectivesMap;
        private static readonly Func<SyntaxTree, SmallConcurrentSetOfInts> s_createSetCallback = t => new SmallConcurrentSetOfInts();

        private ConcurrentDictionary<SyntaxTree, SmallConcurrentSetOfInts> TreeToUsedImportDirectivesMap
        {
            get
            {
                return RoslynLazyInitializer.EnsureInitialized(ref _lazyTreeToUsedImportDirectivesMap);
            }
        }

        internal void MarkImportDirectiveAsUsed(SyntaxNode node)
        {
            MarkImportDirectiveAsUsed(node.SyntaxTree, node.Span.Start);
        }

        internal void MarkImportDirectiveAsUsed(SyntaxTree? syntaxTree, int position)
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

            SmallConcurrentSetOfInts? usedImports;
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
        /// Compare two source locations, using their containing trees, and then by Span.First within a tree.
        /// Can be used to get a total ordering on declarations, for example.
        /// </summary>
        internal abstract int CompareSourceLocations(SyntaxReference loc1, SyntaxReference loc2);

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
        internal TLocation? FirstSourceLocation<TLocation>(ImmutableArray<TLocation> locations)
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
            if (source == null || destination == null) return this.AssemblyName ?? "";
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

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        /// <summary>
        /// Return true if there is a source declaration symbol name that matches the provided name.
        /// This may be faster than <see cref="ContainsSymbolsWithName(Func{string, bool},
        /// SymbolFilter, CancellationToken)"/> when predicate is just a simple string check.
        /// <paramref name="name"/> is case sensitive or not depending on the target language.
        /// </summary>
        public abstract bool ContainsSymbolsWithName(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Return source declaration symbols whose name matches the provided name.  This may be
        /// faster than <see cref="GetSymbolsWithName(Func{string, bool}, SymbolFilter,
        /// CancellationToken)"/> when predicate is just a simple string check.  <paramref
        /// name="name"/> is case sensitive or not depending on the target language.
        /// </summary>
        public abstract IEnumerable<ISymbol> GetSymbolsWithName(string name, SymbolFilter filter = SymbolFilter.TypeAndMember, CancellationToken cancellationToken = default(CancellationToken));
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

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

        internal void MakeTypeMissing(SpecialType type)
        {
            MakeTypeMissing((int)type);
        }

        internal void MakeTypeMissing(WellKnownType type)
        {
            MakeTypeMissing((int)type);
        }

        private void MakeTypeMissing(int type)
        {
            if (_lazyMakeWellKnownTypeMissingMap == null)
            {
                _lazyMakeWellKnownTypeMissingMap = new SmallDictionary<int, bool>();
            }

            _lazyMakeWellKnownTypeMissingMap[(int)type] = true;
        }

        internal bool IsTypeMissing(SpecialType type)
        {
            return IsTypeMissing((int)type);
        }

        internal bool IsTypeMissing(WellKnownType type)
        {
            return IsTypeMissing((int)type);
        }

        private bool IsTypeMissing(int type)
        {
            return _lazyMakeWellKnownTypeMissingMap != null && _lazyMakeWellKnownTypeMissingMap.ContainsKey((int)type);
        }

        /// <summary>
        /// Given a <see cref="Diagnostic"/> reporting unreferenced <see cref="AssemblyIdentity"/>s, returns
        /// the actual <see cref="AssemblyIdentity"/> instances that were not referenced.
        /// </summary>
        public ImmutableArray<AssemblyIdentity> GetUnreferencedAssemblyIdentities(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (!IsUnreferencedAssemblyIdentityDiagnosticCode(diagnostic.Code))
            {
                return ImmutableArray<AssemblyIdentity>.Empty;
            }

            var builder = ArrayBuilder<AssemblyIdentity>.GetInstance();

            foreach (var argument in diagnostic.Arguments)
            {
                if (argument is AssemblyIdentity id)
                {
                    builder.Add(id);
                }
            }

            return builder.ToImmutableAndFree();
        }

        internal abstract bool IsUnreferencedAssemblyIdentityDiagnosticCode(int code);

        /// <summary>
        /// Returns the required language version found in a <see cref="Diagnostic"/>, if any is found.
        /// Returns null if none is found.
        /// </summary>
        public static string? GetRequiredLanguageVersion(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            bool found = false;
            string? foundVersion = null;
            if (diagnostic.Arguments != null)
            {
                foreach (var argument in diagnostic.Arguments)
                {
                    if (argument is RequiredLanguageVersion versionDiagnostic)
                    {
                        Debug.Assert(!found); // only one required language version in a given diagnostic
                        found = true;
                        foundVersion = versionDiagnostic.ToString();
                    }
                }
            }

            return foundVersion;
        }
    }
}

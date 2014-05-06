// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Instrumentation;
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
        // Inverse of syntaxTrees array (i.e. maps tree to index)
        internal readonly ImmutableDictionary<SyntaxTree, int> syntaxTreeOrdinalMap;

        /// <summary>
        /// Returns true if this is a case sensitive compilation, false otherwise.  Case sensitivity
        /// affects compilation features such as name lookup as well as choosing what names to emit
        /// when there are multiple different choices (for example between a virtual method and an
        /// override).
        /// </summary>
        public abstract bool IsCaseSensitive { get; }

        internal Compilation(
            string name,
            ImmutableArray<MetadataReference> references,
            Type submissionReturnType,
            Type hostObjectType,
            bool isSubmission,
            ImmutableDictionary<SyntaxTree, int> syntaxTreeOrdinalMap,
            AsyncQueue<CompilationEvent> eventQueue)
        {
            Debug.Assert(!references.IsDefault);

            this.AssemblyName = name;
            this.ExternalReferences = references;
            this.syntaxTreeOrdinalMap = syntaxTreeOrdinalMap;
            this.EventQueue = eventQueue;

            if (isSubmission)
            {
                this.lazySubmissionSlotIndex = SubmissionSlotIndexToBeAllocated;
                this.SubmissionReturnType = submissionReturnType ?? typeof(object);
                this.HostObjectType = hostObjectType;
            }
            else
            {
                this.lazySubmissionSlotIndex = SubmissionSlotIndexNotApplicable;
            }
        }

        /// <summary>
        /// Gets the source language ("C#" or "Visual Basic").
        /// </summary>
        public abstract string Language { get; }

        internal static void ValidateSubmissionParameters(Compilation previousSubmission, Type returnType, ref Type hostObjectType)
        {
            if (hostObjectType != null && !IsValidHostObjectType(hostObjectType))
            {
                throw new ArgumentException(CodeAnalysisResources.ReturnTypeCannotBeValuePointerbyRefOrOpen, "hostObjectType");
            }

            if (returnType != null && !IsValidSubmissionReturnType(returnType))
            {
                throw new ArgumentException(CodeAnalysisResources.ReturnTypeCannotBeVoidByRefOrOpen, "returnType");
            }

            if (previousSubmission != null)
            {
                if (hostObjectType == null)
                {
                    hostObjectType = previousSubmission.HostObjectType;
                }
                else if (hostObjectType != previousSubmission.HostObjectType)
                {
                    throw new ArgumentException(CodeAnalysisResources.TypeMustBeSameAsHostObjectTypeOfPreviousSubmission, "hostObjectType");
                }

                // Force the previous submission to be analyzed. This is required for anonymous types unification.
                if (previousSubmission.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
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
                throw new ArgumentException(CodeAnalysisResources.InvalidOutputKindForSubmission, "options");
            }

            if (options.CryptoKeyContainer != null || options.CryptoKeyFile != null || options.DelaySign != null)
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidCompilationOptions, "options");
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
        public abstract Compilation WithEventQueue(AsyncQueue<CompilationEvent> eventQueue);

        /// <summary>
        /// Gets a new <see cref="SemanticModel"/> for the specified syntax tree.
        /// </summary>
        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
        {
            return CommonGetSemanticModel(syntaxTree);
        }

        protected abstract SemanticModel CommonGetSemanticModel(SyntaxTree syntaxTree);

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
        /// Unless <see cref="P:CompilationOptions.ModuleName"/> specifies otherwise the module name
        /// written to metadata is <see cref="P:Name"/> with an extension based upon <see cref="P:CompilationOptions.OutputKind"/>.
        /// </remarks>
        public string AssemblyName { get; private set; }

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
        private int lazySubmissionSlotIndex;
        private const int SubmissionSlotIndexNotApplicable = -3;
        private const int SubmissionSlotIndexToBeAllocated = -2;

        /// <summary>
        /// True if the compilation represents an interactive submission.
        /// </summary>
        internal bool IsSubmission
        {
            get
            {
                return lazySubmissionSlotIndex != SubmissionSlotIndexNotApplicable;
            }
        }

        /// <summary>
        /// Gets or allocates a runtime submission slot index for this compilation.
        /// </summary>
        /// <returns>Non-negative integer if this is a submission and it or a previous submission contains code, negative integer otherwise.</returns>
        internal int GetSubmissionSlotIndex()
        {
            if (lazySubmissionSlotIndex == SubmissionSlotIndexToBeAllocated)
            {
                // TODO (tomat): remove recursion
                int lastSlotIndex = (PreviousSubmission != null) ? PreviousSubmission.GetSubmissionSlotIndex() : -1;
                lazySubmissionSlotIndex = HasCodeToEmit() ? lastSlotIndex + 1 : lastSlotIndex;
            }

            return lazySubmissionSlotIndex;
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
        internal Type SubmissionReturnType { get; private set; }

        internal static bool IsValidSubmissionReturnType(Type type)
        {
            return !(type == typeof(void) || type.IsByRef || type.GetTypeInfo().ContainsGenericParameters);
        }

        /// <summary>
        /// The type of the host object or null if not specified for this compilation.
        /// </summary>
        internal Type HostObjectType { get; private set; }

        internal static bool IsValidHostObjectType(Type type)
        {
            var info = type.GetTypeInfo();
            return !(info.IsValueType || info.IsPointer || info.IsByRef || info.ContainsGenericParameters);
        }

        /// <summary>
        /// Returns the type of the submission return value.
        /// </summary>
        /// <param name="hasValue">
        /// True if the submission has a return value, i.e. if the submission
        /// ends with an expression statement.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The compilation doesn't represent a submission
        /// (<see cref="IsSubmission"/> return false).
        /// </exception>
        /// <returns>
        /// Null if the type of the last expression is unknown, 
        /// <see cref="void"/> if the type of the last expression statement is
        /// void or if the submission is not an expression statement, or
        /// otherwise the type of the last expression.
        /// </returns>
        /// <remarks>
        /// Note that the return type is <see cref="void"/> if the last
        /// statement is a non-expression statement e.g.,
        /// <code>System.Console.WriteLine();</code>
        /// and if the statement is an expression statement of type void e.g,
        /// <code>System.Console.WriteLine()</code>. However,
        /// <paramref name="hasValue"/> is false in the former case and true
        /// in the latter.
        /// </remarks>
        public ITypeSymbol GetSubmissionResultType(out bool hasValue)
        {
            return CommonGetSubmissionResultType(out hasValue);
        }

        protected abstract ITypeSymbol CommonGetSubmissionResultType(out bool hasValue);

        /// <summary>
        /// The previous submission compilation, or null if either this
        /// compilation doesn't represent a submission or the submission is the
        /// first submission in a submission chain.
        /// </summary>
        public Compilation PreviousSubmission { get { return CommonPreviousSubmission; } }

        protected abstract Compilation CommonPreviousSubmission { get; }

        /// <summary>
        /// Returns a new compilation with the given compilation set as the
        /// previous submission.
        /// </summary>
        public Compilation WithPreviousSubmission(Compilation newPreviousSubmission)
        {
            return CommonWithPreviousSubmission(newPreviousSubmission);
        }

        protected abstract Compilation CommonWithPreviousSubmission(Compilation newPreviousSubmission);

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
        public ImmutableArray<MetadataReference> ExternalReferences { get; private set; }

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
        internal abstract IDictionary<string, MetadataReference> ReferenceDirectiveMap { get; }

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
                throw new ArgumentNullException("references");
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
                throw new ArgumentNullException("references");
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
                    throw new ArgumentException(String.Format("MetadataReference '{0}' not found to remove", r), "references");
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
                throw new ArgumentNullException("oldReference");
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
            return CommonGetMetadataReference(assemblySymbol);
        }

        protected abstract MetadataReference CommonGetMetadataReference(IAssemblySymbol assemblySymbol);

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
        /// <see cref="T:EmitResult"/>.
        /// </summary>
        public abstract ImmutableArray<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken));

        internal abstract CommonMessageProvider MessageProvider { get; }

        /// <param name="accumulator">Bag to which filtered diagnostics will be added.</param>
        /// <param name="incoming">Diagnostics to be filtered.</param>
        /// <returns>True if there were no errors or warnings-as-errors.</returns>
        internal abstract bool FilterAndAppendAndFreeDiagnostics(DiagnosticBag accumulator, ref DiagnosticBag incoming);

        /// <summary>
        /// Modifies the incoming diagnostic, for example escalating its severity, or discarding it (returning null).
        /// </summary>
        /// <param name="diagnostic"></param>
        /// <returns>The modified diagnostic, or null</returns>
        internal abstract Diagnostic FilterDiagnostic(Diagnostic diagnostic);

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
            List<RESOURCE> resources = null;

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

        /// <summary>
        /// Constructs the module serialization properties out of the compilation options of this compilation.
        /// </summary>
        internal ModulePropertiesForSerialization ConstructModuleSerializationProperties(string targetRuntimeVersion, Guid moduleVersionId = default(Guid))
        {
            CompilationOptions options = this.Options;

            Platform platform = options.Platform;

            if (!platform.IsValid())
            {
                platform = Platform.AnyCpu;
            }

            bool requires64bits = platform.Requires64Bit();

            ushort fileAlignment;
            if (options.FileAlignment == 0 || !CompilationOptions.IsValidFileAlignment(options.FileAlignment))
            {
                fileAlignment = requires64bits
                    ? ModulePropertiesForSerialization.DefaultFileAlignment64Bit
                    : ModulePropertiesForSerialization.DefaultFileAlignment32Bit;
            }
            else
            {
                fileAlignment = (ushort)options.FileAlignment;
            }

            ulong baseAddress = unchecked(options.BaseAddress + 0x8000) & (requires64bits ? 0xffffffffffff0000 : 0x00000000ffff0000);

            // cover values smaller than 0x8000, overflow and default value 0):
            if (baseAddress == 0)
            {
                OutputKind outputKind = options.OutputKind;

                if (outputKind == OutputKind.ConsoleApplication ||
                    outputKind == OutputKind.WindowsApplication ||
                    outputKind == OutputKind.WindowsRuntimeApplication)
                {
                    baseAddress = (requires64bits) ? ModulePropertiesForSerialization.DefaultExeBaseAddress64Bit : ModulePropertiesForSerialization.DefaultExeBaseAddress32Bit;
                }
                else
                {
                    baseAddress = (requires64bits) ? ModulePropertiesForSerialization.DefaultDllBaseAddress64Bit : ModulePropertiesForSerialization.DefaultDllBaseAddress32Bit;
                }
            }

            ulong sizeOfHeapCommit = requires64bits
                ? ModulePropertiesForSerialization.DefaultSizeOfHeapCommit64Bit
                : ModulePropertiesForSerialization.DefaultSizeOfHeapCommit32Bit;

            // Dev10 always uses the default value for 32bit for sizeOfHeapReserve.
            // check with link -dump -headers <filename>
            ulong sizeOfHeapReserve = ModulePropertiesForSerialization.DefaultSizeOfHeapReserve32Bit;

            ulong sizeOfStackReserve = requires64bits
                ? ModulePropertiesForSerialization.DefaultSizeOfStackReserve64Bit
                : ModulePropertiesForSerialization.DefaultSizeOfStackReserve32Bit;

            ulong sizeOfStackCommit = requires64bits
                ? ModulePropertiesForSerialization.DefaultSizeOfStackCommit64Bit
                : ModulePropertiesForSerialization.DefaultSizeOfStackCommit32Bit;

            SubsystemVersion subsystemVer = (options.SubsystemVersion.Equals(SubsystemVersion.None) || !options.SubsystemVersion.IsValid)
                ? SubsystemVersion.Default(options.OutputKind.IsValid() ? options.OutputKind : OutputKind.DynamicallyLinkedLibrary, platform)
                : options.SubsystemVersion;

            return new ModulePropertiesForSerialization(
                persistentIdentifier: moduleVersionId,
                fileAlignment: fileAlignment,
                targetRuntimeVersion: targetRuntimeVersion,
                platform: platform,
                trackDebugData: false,
                baseAddress: baseAddress,
                sizeOfHeapReserve: sizeOfHeapReserve,
                sizeOfHeapCommit: sizeOfHeapCommit,
                sizeOfStackReserve: sizeOfStackReserve,
                sizeOfStackCommit: sizeOfStackCommit,
                enableHighEntropyVA: options.HighEntropyVirtualAddressSpace,
                strongNameSigned: this.ShouldBeSigned,
                configureToExecuteInAppContainer: options.OutputKind == OutputKind.WindowsRuntimeApplication,
                subsystemVersion: subsystemVer);
        }

        #region Emit

        protected bool ShouldBeSigned
        {
            get
            {
                //A module cannot be signed. The native compiler allowed one to create a netmodule with an AssemblyKeyFile 
                //or Container attribute (or specify a key via the cmd line). When the module was linked into an assembly,
                //alink would sign the assembly. So rather than give an error we just don't sign when outputting a module.

                return !IsDelaySign
                    && Options.OutputKind != OutputKind.NetModule
                    && StrongNameKeys.CanSign;
            }
        }

        /// <summary>
        /// Return true if the compilation contains any code or types.
        /// </summary>
        protected abstract bool HasCodeToEmit();

        internal abstract bool IsDelaySign { get; }
        internal abstract StrongNameKeys StrongNameKeys { get; }
        internal abstract FunctionId EmitFunctionId { get; }

        internal abstract CommonPEModuleBuilder CreateModuleBuilder(
            string outputName,
            IEnumerable<ResourceDescription> manifestResources,
            Func<IAssemblySymbol, AssemblyIdentity> assemblySymbolMapper,
            CancellationToken cancellationToken,
            CompilationTestData testData,
            DiagnosticBag diagnostics,
            bool metadataOnly);

        // TODO: private protected
        internal abstract bool CompileImpl(
            CommonPEModuleBuilder moduleBuilder,
            string outputName,
            Stream win32Resources,
            Stream xmlDocStream,
            CancellationToken cancellationToken,
            bool generateDebugInfo,
            DiagnosticBag diagnostics,
            Predicate<ISymbol> filterOpt);

        internal bool Compile(
            CommonPEModuleBuilder moduleBuilder,
            string outputName,
            Stream win32Resources,
            Stream xmlDocStream,
            CancellationToken cancellationToken,
            bool generateDebugInfo,
            DiagnosticBag diagnostics,
            Predicate<ISymbol> filterOpt)
        {
            try
            {
                return CompileImpl(
                    moduleBuilder,
                    outputName,
                    win32Resources,
                    xmlDocStream,
                    cancellationToken,
                    generateDebugInfo,
                    diagnostics,
                    filterOpt);
            }
            finally
            {
                moduleBuilder.CompilationFinished();
            }
        }

        internal void EnsureAnonymousTypeTemplates(CancellationToken cancellationToken)
        {
            if (this.GetSubmissionSlotIndex() >= 0 && HasCodeToEmit())
            {
                if (!this.CommonAnonymousTypeManager.AreTemplatesSealed)
                {
                    var discardedDiagnostics = DiagnosticBag.GetInstance();

                    var moduleBeingBuilt = this.CreateModuleBuilder(
                        outputName: null,
                        manifestResources: null,
                        assemblySymbolMapper: null,
                        cancellationToken: cancellationToken,
                        testData: null,
                        diagnostics: discardedDiagnostics,
                        metadataOnly: false);

                    if (moduleBeingBuilt != null)
                    {
                        Compile(
                            moduleBeingBuilt,
                            outputName: null,
                            win32Resources: null,
                            xmlDocStream: null,
                            cancellationToken: cancellationToken,
                            generateDebugInfo: false,
                            diagnostics: discardedDiagnostics,
                            filterOpt: null);
                    }

                    discardedDiagnostics.Free();
                }

                Debug.Assert(this.CommonAnonymousTypeManager.AreTemplatesSealed);
            }
            else if (this.PreviousSubmission != null)
            {
                this.PreviousSubmission.EnsureAnonymousTypeTemplates(cancellationToken);
            }
        }

        /// <summary>
        /// Emit the IL for the compiled source code into the specified stream.
        /// </summary>
        /// <param name="executableStream">Stream to which the compilation will be written.</param>
        /// <param name="outputName">Name of the compilation: file name and extension.  Null to use the existing output name.
        /// CAUTION: If this is set to a (non-null) value other than the existing compilation output name, then internals-visible-to
        /// and assembly references may not work as expected.  In particular, things that were visible at bind time, based on the 
        /// name of the compilation, may not be visible at runtime and vice-versa.
        /// </param>
        /// <param name="pdbFilePath">
        /// The name of the PDB file embedded in the PE image. 
        /// If not specified, the file name of the source module with an extension changed to "pdb" is used.
        /// Ignored unless <paramref name="pdbStream"/> is non-null.
        /// </param>
        /// <param name="pdbStream">Stream to which the compilation's debug info will be written.  Null to forego PDB generation.</param>
        /// <param name="xmlDocStream">Stream to which the compilation's XML documentation will be written.  Null to forego XML generation.</param>
        /// <param name="cancellationToken">To cancel the emit process.</param>
        /// <param name="win32Resources">Stream from which the compilation's Win32 resources will be read (in RES format).  
        /// Null to indicate that there are none. The RES format begins with a null resource entry.</param>
        /// <param name="manifestResources">List of the compilation's managed resources.  Null to indicate that there are none.</param>
        public EmitResult Emit(
            Stream executableStream,
            string outputName = null,
            string pdbFilePath = null,
            Stream pdbStream = null,
            Stream xmlDocStream = null,
            CancellationToken cancellationToken = default(CancellationToken),
            Stream win32Resources = null,
            IEnumerable<ResourceDescription> manifestResources = null)
        {
            if (executableStream == null)
            {
                throw new ArgumentNullException("executableStream");
            }

            return Emit(
                executableStream,
                outputName,
                pdbFilePath,
                pdbStream,
                xmlDocStream,
                cancellationToken,
                win32Resources,
                manifestResources,
                metadataOnly: false,
                testData: null);
        }

        /// <summary>
        /// Emit the IL for the compilation into the specified stream.
        /// </summary>
        /// <param name="outputPath">Path of the file to which the compilation will be written.</param>
        /// <param name="pdbPath">Path of the file to which the compilation's debug info will be written.
        /// Also embedded in the output file.  Null to forego PDB generation.
        /// </param>
        /// <param name="xmlDocPath">Path of the file to which the compilation's XML documentation will be written.  Null to forego XML generation.</param>
        /// <param name="cancellationToken">To cancel the emit process.</param>
        /// <param name="win32ResourcesPath">Path of the file from which the compilation's Win32 resources will be read (in RES format).  
        /// Null to indicate that there are none.</param>
        /// <param name="manifestResources">List of the compilation's managed resources.  Null to indicate that there are none.</param>
        public EmitResult Emit(
            string outputPath,
            string pdbPath = null,
            string xmlDocPath = null,
            CancellationToken cancellationToken = default(CancellationToken),
            string win32ResourcesPath = null,
            IEnumerable<ResourceDescription> manifestResources = null)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("outputPath");
            }

            using (var outputStream = File.Create(outputPath))
            using (var pdbStream = (pdbPath == null ? null : File.Create(pdbPath)))
            using (var xmlDocStream = (xmlDocPath == null ? null : File.Create(xmlDocPath)))
            using (var win32ResourcesStream = (win32ResourcesPath == null ? null : File.OpenRead(win32ResourcesPath)))
            {
                return Emit(
                    outputStream,
                    outputName: null,
                    pdbFilePath: pdbPath,
                    pdbStream: pdbStream,
                    xmlDocStream: xmlDocStream,
                    cancellationToken: cancellationToken,
                    win32Resources: win32ResourcesStream,
                    manifestResources: manifestResources);
            }
        }

        /// <summary>
        /// Emits the IL for the symbol declarations into the specified stream. Useful for emitting
        /// information for cross-language modeling of code. This emits what it can even if there
        /// are errors.
        /// </summary>
        /// <param name="metadataStream">Stream to which the compilation's metadata will be written.</param>
        /// <param name="xmlDocStream">Stream to which the compilation's XML documentation will be written.  Null to forego XML generation.</param>
        /// <param name="outputName">Name of the compilation: file name and extension.  Null to use the existing output name.
        /// CAUTION: If this is set to a (non-null) value other than the existing compilation output name, then internals-visible-to
        /// and assembly references may not work as expected.  In particular, things that were visible at bind time, based on the 
        /// name of the compilation, may not be visible at runtime and vice-versa.
        /// </param>
        /// <param name="cancellationToken">To cancel the emit process.</param>
        public EmitResult EmitMetadataOnly(
            Stream metadataStream,
            string outputName = null,
            Stream xmlDocStream = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (metadataStream == null)
            {
                throw new ArgumentNullException("metadataStream");
            }

            return Emit(
                metadataStream,
                outputName,
                pdbFilePath: null,
                pdbStream: null,
                xmlDocStream: xmlDocStream,
                cancellationToken: cancellationToken,
                win32Resources: null,
                manifestResources: null,
                metadataOnly: true,
                testData: null);
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
            ICollection<uint> updatedMethodTokens,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (baseline == null)
            {
                throw new ArgumentNullException("baseline");
            }

            // TODO: check if baseline is an assembly manifest module/netmodule
            // Do we support EnC on netmodules?

            if (edits == null)
            {
                throw new ArgumentNullException("edits");
            }

            if (metadataStream == null)
            {
                throw new ArgumentNullException("metadataStream");
            }

            if (ilStream == null)
            {
                throw new ArgumentNullException("ilStream");
            }

            if (pdbStream == null)
            {
                throw new ArgumentNullException("pdbStream");
            }

            return this.EmitDifference(baseline, edits, metadataStream, ilStream, pdbStream, updatedMethodTokens, null, cancellationToken);
        }

        internal abstract EmitDifferenceResult EmitDifference(
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            ICollection<uint> updatedMethodTokens,
            CompilationTestData testData,
            CancellationToken cancellationToken);

        /// <summary>
        /// This overload is only intended to be directly called by tests that want to pass <paramref name="testData"/>.
        /// The map is used for storing a list of methods and their associated IL.
        /// </summary>
        /// <returns>True if emit succeeded.</returns>
        internal EmitResult Emit(
            Stream executableStream,
            string outputName,
            string pdbFilePath,
            Stream pdbStream,
            Stream xmlDocStream,
            CancellationToken cancellationToken,
            Stream win32Resources,
            IEnumerable<ResourceDescription> manifestResources,
            bool metadataOnly,
            CompilationTestData testData)
        {
            Debug.Assert(executableStream != null);

            using (Logger.LogBlock(EmitFunctionId, message: this.AssemblyName, cancellationToken: cancellationToken))
            {
                if (outputName != null)
                {
                    MetadataHelpers.ValidateAssemblyOrModuleName(outputName, "outputName");
                }

                DiagnosticBag diagnostics = new DiagnosticBag();
                if (Options.OutputKind == OutputKind.NetModule && manifestResources != null)
                {
                    foreach (ResourceDescription res in manifestResources)
                    {
                        if (res.FileName != null)
                        {
                            // Modules can have only embedded resources, not linked ones.
                            diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_ResourceInModule, Location.None));

                            return ToEmitResultAndFree(diagnostics, success: false);
                        }
                    }
                }

                var moduleBeingBuilt = this.CreateModuleBuilder(
                    outputName,
                    manifestResources,
                    null,
                    cancellationToken,
                    testData,
                    diagnostics,
                    metadataOnly);

                if (moduleBeingBuilt == null)
                {
                    return ToEmitResultAndFree(diagnostics, success: false);
                }

                if (!this.Compile(
                    moduleBeingBuilt,
                    outputName,
                    win32Resources,
                    xmlDocStream,
                    cancellationToken,
                    generateDebugInfo: pdbStream != null,
                    diagnostics: diagnostics,
                    filterOpt: null))
                {
                    return ToEmitResultAndFree(diagnostics, success: false);
                }

                bool success = SerializeToPeStream(
                    (Cci.IModule)moduleBeingBuilt,
                    executableStream,
                    pdbFilePath,
                    pdbStream,
                    (testData != null) ? testData.SymWriterFactory : null,
                    diagnostics,
                    metadataOnly,
                    Options.Optimize,
                    cancellationToken);

                return ToEmitResultAndFree(diagnostics, success);
            }
        }

        private EmitResult ToEmitResultAndFree(DiagnosticBag diagnostics, bool success)
        {
            return new EmitResult(success, diagnostics.ToReadOnlyAndFree());
        }

        internal bool SerializeToPeStream(
            Cci.IModule moduleBeingBuilt,
            Stream executableStream,
            string pdbFileName,
            Stream pdbStream,
            Func<object> testSymWriterFactory,
            DiagnosticBag diagnostics,
            bool metadataOnly,
            bool foldIdenticalMethodBodies,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Common_Compilation_SerializeToPeStream, message: this.AssemblyName, cancellationToken: cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                Cci.PdbWriter pdbWriter = null;
                Stream signingInputStream = null;
                DiagnosticBag metadataDiagnostics = null;
                DiagnosticBag pdbBag = null;

                try
                {
                    if (pdbStream != null)
                    {
                        pdbWriter = new Cci.PdbWriter(pdbFileName ?? PathUtilities.ChangeExtension(SourceModule.Name, "pdb"), pdbStream, testSymWriterFactory);
                    }

                    // Signing can only be done to on-disk files. This is a limitation of the CLR APIs which we use 
                    // to perform strong naming. If this binary is configured to be signed, create a temp file, output to that
                    // then stream that to the stream that this method was called with. Otherwise output to the
                    // stream that this method was called with.

                    Stream outputStream;
                    if (!metadataOnly && ShouldBeSigned)
                    {
                        Debug.Assert(Options.StrongNameProvider != null);

                        signingInputStream = Options.StrongNameProvider.CreateInputStream();
                        outputStream = signingInputStream;
                    }
                    else
                    {
                        signingInputStream = null;
                        outputStream = executableStream;
                    }

                    metadataDiagnostics = DiagnosticBag.GetInstance();
                    try
                    {
                        string deterministicString = this.Feature("deterministic");
                        bool deterministic =  deterministicString != null && deterministicString != "false";
                        Cci.PeWriter.WritePeToStream(
                            new EmitContext(moduleBeingBuilt, null, metadataDiagnostics),
                            this.MessageProvider,
                            outputStream,
                            pdbWriter,
                            metadataOnly,
                            foldIdenticalMethodBodies,
                            deterministic,
                            cancellationToken);
                    }
                    catch (Cci.PdbWritingException ex)
                    {
                        diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.ERR_PdbWritingFailed, Location.None, ex.Message));
                        return false;
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

                    if (signingInputStream != null)
                    {
                        Debug.Assert(Options.StrongNameProvider != null);

                        try
                        {
                            Options.StrongNameProvider.SignAssembly(StrongNameKeys, signingInputStream, executableStream);
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
                    if (pdbWriter != null)
                    {
                        pdbWriter.Dispose();
                        pdbWriter = null;
                    }

                    if (signingInputStream != null)
                    {
                        signingInputStream.Dispose();
                    }

                    if (pdbBag != null)
                    {
                        pdbBag.Free();
                    }

                    if (metadataDiagnostics != null)
                    {
                        metadataDiagnostics.Free();
                    }
                }

                return true;
            }
        }

        private Dictionary<string, string> lazyFeatures;
        internal string Feature(string p)
        {
            if (this.lazyFeatures == null)
            {
                var set = new Dictionary<string, string>();
                if (Options.Features != null)
                {
                    foreach (var s in Options.Features)
                    {
                        string feature = s.ToLowerInvariant();

                        int colon = feature.IndexOf(':');
                        if (colon > 0)
                        {
                            string name = feature.Substring(0, colon);
                            string value = feature.Substring(colon + 1);
                            set[name] = value;
                        }
                        else
                        {
                            set[feature] = "true";
                        }
                    }
                }

                Interlocked.CompareExchange(ref this.lazyFeatures, set, null);
            }

            string v;
            return this.lazyFeatures.TryGetValue(p.ToLowerInvariant(), out v) ? v : null;
        }

        #endregion

        private ConcurrentDictionary<SyntaxTree, SmallConcurrentSetOfInts> lazyTreeToUsedImportDirectivesMap;
        private static readonly Func<SyntaxTree, SmallConcurrentSetOfInts> createSetCallback = t => new SmallConcurrentSetOfInts();

        private ConcurrentDictionary<SyntaxTree, SmallConcurrentSetOfInts> TreeToUsedImportDirectivesMap
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref this.lazyTreeToUsedImportDirectivesMap);
            }
        }

        internal void MarkImportDirectiveAsUsed(SyntaxNode node)
        {
            MarkImportDirectiveAsUsed(node.SyntaxTree, node.Span.Start);
        }

        internal void MarkImportDirectiveAsUsed(SyntaxTree syntaxTree, int position)
        {
            if (syntaxTree != null)
            {
                var set = TreeToUsedImportDirectivesMap.GetOrAdd(syntaxTree, createSetCallback);
                set.Add(position);
            }
        }

        internal bool IsImportDirectiveUsed(SyntaxTree syntaxTree, int position)
        {
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

            return this.syntaxTreeOrdinalMap[tree1] - this.syntaxTreeOrdinalMap[tree2];
        }

        internal int GetSyntaxTreeOrdinal(SyntaxTree tree)
        {
            Debug.Assert(this.ContainsSyntaxTree(tree));
            return this.syntaxTreeOrdinalMap[tree];
        }

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
    }
}
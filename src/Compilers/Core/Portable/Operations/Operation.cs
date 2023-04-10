// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(), nq}}")]
    internal abstract partial class Operation : IOperation
    {
        protected static readonly IOperation s_unset = new EmptyOperation(semanticModel: null, syntax: null!, isImplicit: true);
        private readonly SemanticModel? _owningSemanticModelOpt;

        // this will be lazily initialized. this will be initialized only once
        // but once initialized, will never change
        private IOperation? _parentDoNotAccessDirectly;

        protected Operation(SemanticModel? semanticModel, SyntaxNode syntax, bool isImplicit)
        {
#if DEBUG
            if (semanticModel != null)
            {
                Debug.Assert(semanticModel.ContainingPublicModelOrSelf != null);
                Debug.Assert(semanticModel.ContainingPublicModelOrSelf != semanticModel);
                Debug.Assert(semanticModel.ContainingPublicModelOrSelf.ContainingPublicModelOrSelf == semanticModel.ContainingPublicModelOrSelf);
            }
#endif
            _owningSemanticModelOpt = semanticModel;

            Syntax = syntax;
            IsImplicit = isImplicit;

            _parentDoNotAccessDirectly = s_unset;
        }

        /// <summary>
        /// IOperation that has this operation as a child
        /// </summary>
        public IOperation? Parent
        {
            get
            {
                Debug.Assert(_parentDoNotAccessDirectly != s_unset, "Attempt to access parent node before construction is complete!");
                return _parentDoNotAccessDirectly;
            }
        }

        /// <summary>
        /// Set to True if compiler generated /implicitly computed by compiler code
        /// </summary>
        public bool IsImplicit { get; }

        /// <summary>
        /// Identifies the kind of the operation.
        /// </summary>
        public abstract OperationKind Kind { get; }

        /// <summary>
        /// Syntax that was analyzed to produce the operation.
        /// </summary>
        public SyntaxNode Syntax { get; }

        /// <summary>
        /// Result type of the operation, or null if the operation does not produce a result.
        /// </summary>
        public abstract ITypeSymbol? Type { get; }

        /// <summary>
        /// The source language of the IOperation. Possible values are <see cref="LanguageNames.CSharp"/> and <see cref="LanguageNames.VisualBasic"/>.
        /// </summary>

        public string Language
        {
            // It is an eventual goal to support analyzing IL. At that point, we'll need to detect a null
            // syntax and add a new field to LanguageNames for IL. Until then, though, we'll just assume that
            // syntax is not null and return its language.
            get => Syntax.Language;
        }

        internal abstract CodeAnalysis.ConstantValue? OperationConstantValue { get; }

        /// <summary>
        /// If the operation is an expression that evaluates to a constant value, <see cref="Optional{Object}.HasValue"/> is true and <see cref="Optional{Object}.Value"/> is the value of the expression. Otherwise, <see cref="Optional{Object}.HasValue"/> is false.
        /// </summary>
        public Optional<object?> ConstantValue
        {
            get
            {
                if (OperationConstantValue == null || OperationConstantValue.IsBad)
                {
                    return default(Optional<object?>);
                }

                return new Optional<object?>(OperationConstantValue.Value);
            }
        }

        IEnumerable<IOperation> IOperation.Children => this.ChildOperations;

        /// <inheritdoc/>
        public IOperation.OperationList ChildOperations => new IOperation.OperationList(this);

        internal abstract int ChildOperationsCount { get; }
        internal abstract IOperation GetCurrent(int slot, int index);
        /// <summary>
        /// A slot of -1 means start at the beginning.
        /// </summary>
        internal abstract (bool hasNext, int nextSlot, int nextIndex) MoveNext(int previousSlot, int previousIndex);
        /// <summary>
        /// A slot of int.MaxValue means start from the end.
        /// </summary>
        internal abstract (bool hasNext, int nextSlot, int nextIndex) MoveNextReversed(int previousSlot, int previousIndex);

        SemanticModel? IOperation.SemanticModel => _owningSemanticModelOpt?.ContainingPublicModelOrSelf;

        /// <summary>
        /// Gets the owning semantic model for this operation node.
        /// Note that this may be different than <see cref="IOperation.SemanticModel"/>, which
        /// is the semantic model on which <see cref="SemanticModel.GetOperation(SyntaxNode, CancellationToken)"/> was invoked
        /// to create this node.
        /// </summary>
        internal SemanticModel? OwningSemanticModel => _owningSemanticModelOpt;

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        protected void SetParentOperation(IOperation? parent)
        {
            Debug.Assert(_parentDoNotAccessDirectly == s_unset);
            Debug.Assert(parent != s_unset);
            _parentDoNotAccessDirectly = parent;

            // tree must belong to same semantic model if parent is given
            Debug.Assert(parent == null || ((Operation)parent).OwningSemanticModel == OwningSemanticModel);
        }

        [return: NotNullIfNotNull(nameof(operation))]
        public static T? SetParentOperation<T>(T? operation, IOperation? parent) where T : IOperation
        {
            // For simplicity of implementation of derived types, we handle `null` children, as some children
            // are optional.
            (operation as Operation)?.SetParentOperation(parent);
            return operation;
        }

        public static ImmutableArray<T> SetParentOperation<T>(ImmutableArray<T> operations, IOperation? parent) where T : IOperation
        {
            // check quick bail out case first
            if (operations.Length == 0)
            {
                // no element
                return operations;
            }

            foreach (var operation in operations)
            {
                SetParentOperation(operation, parent);
            }

            return operations;
        }

        [Conditional("DEBUG")]
        internal static void VerifyParentOperation(IOperation? parent, IOperation child)
        {
            if (child is object)
            {
                Debug.Assert((object?)child.Parent == parent);
            }
        }

        [Conditional("DEBUG")]
        internal static void VerifyParentOperation<T>(IOperation? parent, ImmutableArray<T> children) where T : IOperation
        {
            Debug.Assert(!children.IsDefault);
            foreach (var child in children)
            {
                VerifyParentOperation(parent, child);
            }
        }

        private string GetDebuggerDisplay() => $"{GetType().Name} Type: {(Type is null ? "null" : Type)}";
    }
}

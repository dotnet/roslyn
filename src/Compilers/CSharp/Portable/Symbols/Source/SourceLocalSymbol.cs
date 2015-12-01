// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a local variable in a method body.
    /// </summary>
    internal class SourceLocalSymbol : LocalSymbol
    {
        protected readonly Binder binder;

        /// <summary>
        /// Might not be a method symbol.
        /// </summary>
        private readonly Symbol _containingSymbol;

        private readonly SyntaxToken _identifierToken;
        private readonly ImmutableArray<Location> _locations;
        private readonly RefKind _refKind;
        private readonly TypeSyntax _typeSyntax;
        private readonly LocalDeclarationKind _declarationKind;
        private TypeSymbol _type;

        /// <summary>
        /// There are three ways to initialize a fixed statement local:
        ///   1) with an address;
        ///   2) with an array (or fixed-size buffer); or
        ///   3) with a string.
        /// 
        /// In the first two cases, the resulting local will be emitted with a "pinned" modifier.
        /// In the third case, it is not the fixed statement local but a synthesized temp that is pinned.  
        /// Unfortunately, we can't distinguish these cases when the local is declared; we only know
        /// once we have bound the initializer.
        /// </summary>
        /// <remarks>
        /// CompareExchange doesn't support bool, so use an int.  First bit is true/false, second bit 
        /// is read/unread (debug-only).
        /// </remarks>
        private int _isSpecificallyNotPinned;

        private SourceLocalSymbol(
            Symbol containingSymbol,
            Binder binder,
            RefKind refKind,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind declarationKind)
        {
            Debug.Assert(identifierToken.Kind() != SyntaxKind.None);
            Debug.Assert(declarationKind != LocalDeclarationKind.None);

            this.binder = binder;
            this._containingSymbol = containingSymbol;
            this._identifierToken = identifierToken;
            this._refKind = refKind;
            this._typeSyntax = typeSyntax;
            this._declarationKind = declarationKind;

            // create this eagerly as it will always be needed for the EnsureSingleDefinition
            _locations = ImmutableArray.Create<Location>(identifierToken.GetLocation());
        }

        public static SourceLocalSymbol MakeForeachLocal(
            MethodSymbol containingMethod,
            ForEachLoopBinder binder,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            ExpressionSyntax collection)
        {
            return new ForEachLocal(containingMethod, binder, typeSyntax, identifierToken, collection, LocalDeclarationKind.ForEachIterationVariable);
        }

        public static SourceLocalSymbol MakeLocal(
            Symbol containingSymbol,
            Binder binder,
            RefKind refKind,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind declarationKind,
            EqualsValueClauseSyntax initializer = null)
        {
            Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
            return (initializer == null) ?
                new SourceLocalSymbol(containingSymbol, binder, refKind, typeSyntax, identifierToken, declarationKind) :
                new LocalWithInitializer(containingSymbol, binder, refKind, typeSyntax, identifierToken, initializer, declarationKind);
        }

        internal override bool IsImportedFromMetadata
        {
            get { return false; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return _declarationKind; }
        }

        internal override SynthesizedLocalKind SynthesizedKind
        {
            get { return SynthesizedLocalKind.UserDefined; }
        }

        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool IsPinned
        {
            get
            {
#if DEBUG
                if ((_isSpecificallyNotPinned & 2) == 0)
                {
                    Interlocked.CompareExchange(ref _isSpecificallyNotPinned, _isSpecificallyNotPinned | 2, _isSpecificallyNotPinned);
                    Debug.Assert((_isSpecificallyNotPinned & 2) == 2, "Regardless of which thread won, the read bit should be set.");
                }
#endif
                return _declarationKind == LocalDeclarationKind.FixedVariable && (_isSpecificallyNotPinned & 1) == 0;
            }
        }

        internal void SetSpecificallyNotPinned()
        {
            Debug.Assert((_isSpecificallyNotPinned & 2) == 0, "Shouldn't be writing after first read.");
            Interlocked.CompareExchange(ref _isSpecificallyNotPinned, _isSpecificallyNotPinned | 1, _isSpecificallyNotPinned);
            Debug.Assert((_isSpecificallyNotPinned & 1) == 1, "Regardless of which thread won, the flag bit should be set.");
        }

        internal virtual void SetReturnable()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        /// <summary>
        /// Gets the name of the local variable.
        /// </summary>
        public override string Name
        {
            get
            {
                return _identifierToken.ValueText;
            }
        }

        // Get the identifier token that defined this local symbol. This is useful for robustly
        // checking if a local symbol actually matches a particular definition, even in the presence
        // of duplicates.
        internal override SyntaxToken IdentifierToken
        {
            get
            {
                return _identifierToken;
            }
        }

        public override TypeSymbol Type
        {
            get
            {
                if ((object)_type == null)
                {
                    TypeSymbol localType = GetTypeSymbol();
                    SetTypeSymbol(localType);
                }

                return _type;
            }
        }

        public bool IsVar
        {
            get
            {
                if (_typeSyntax.IsVar)
                {
                    bool isVar;
                    TypeSymbol declType = this.binder.BindType(_typeSyntax, new DiagnosticBag(), out isVar);
                    return isVar;
                }

                return false;
            }
        }

        private TypeSymbol GetTypeSymbol()
        {
            var diagnostics = DiagnosticBag.GetInstance();

            Binder typeBinder = this.binder;

            bool isVar;
            TypeSymbol declType = typeBinder.BindType(_typeSyntax, diagnostics, out isVar);

            if (isVar)
            {
                TypeSymbol inferredType = InferTypeOfVarVariable(diagnostics);

                // If we got a valid result that was not void then use the inferred type
                // else create an error type.
                if ((object)inferredType != null &&
                    inferredType.SpecialType != SpecialType.System_Void)
                {
                    declType = inferredType;
                }
                else
                {
                    declType = typeBinder.CreateErrorType("var");
                }
            }

            Debug.Assert((object)declType != null);

            diagnostics.Free();
            return declType;
        }

        protected virtual TypeSymbol InferTypeOfVarVariable(DiagnosticBag diagnostics)
        {
            return null;
        }

        internal void SetTypeSymbol(TypeSymbol newType)
        {
            TypeSymbol originalType = _type;

            // In the event that we race to set the type of a local, we should
            // always deduce the same type, or deduce that the type is an error.

            Debug.Assert((object)originalType == null ||
                originalType.IsErrorType() && newType.IsErrorType() ||
                originalType == newType);

            if ((object)originalType == null)
            {
                Interlocked.CompareExchange(ref _type, newType, null);
            }
        }

        /// <summary>
        /// Gets the locations where the local symbol was originally defined in source.
        /// There should not be local symbols from metadata, and there should be only one local variable declared.
        /// TODO: check if there are multiple same name local variables - error symbol or local symbol?
        /// </summary>
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _locations;
            }
        }

        internal sealed override SyntaxNode GetDeclaratorSyntax()
        {
            return _identifierToken.Parent;
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                SyntaxNode node = _identifierToken.Parent;
#if DEBUG
                switch (_declarationKind)
                {
                    case LocalDeclarationKind.RegularVariable:
                    case LocalDeclarationKind.Constant:
                    case LocalDeclarationKind.FixedVariable:
                    case LocalDeclarationKind.UsingVariable:
                    case LocalDeclarationKind.ForInitializerVariable:
                        Debug.Assert(node is VariableDeclaratorSyntax);
                        break;

                    case LocalDeclarationKind.ForEachIterationVariable:
                        Debug.Assert(node is ForEachStatementSyntax);
                        break;

                    case LocalDeclarationKind.CatchVariable:
                        Debug.Assert(node is CatchDeclarationSyntax);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_declarationKind);
                }
#endif
                return ImmutableArray.Create(node.GetReference());
            }
        }

        internal override bool IsCompilerGenerated
        {
            get { return false; }
        }

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics)
        {
            return null;
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

        internal override RefKind RefKind
        {
            get { return _refKind; }
        }

        public sealed override bool Equals(object obj)
        {
            if (obj == (object)this)
            {
                return true;
            }

            var symbol = obj as SourceLocalSymbol;
            return (object)symbol != null
                && symbol._identifierToken.Equals(_identifierToken)
                && Equals(symbol._containingSymbol, _containingSymbol);
        }

        public sealed override int GetHashCode()
        {
            return Hash.Combine(_identifierToken.GetHashCode(), _containingSymbol.GetHashCode());
        }

        private sealed class LocalWithInitializer : SourceLocalSymbol
        {
            private readonly EqualsValueClauseSyntax _initializer;

            /// <summary>
            /// Store the constant value and the corresponding diagnostics together
            /// to avoid having the former set by one thread and the latter set by
            /// another.
            /// </summary>
            private EvaluatedConstant _constantTuple;

            /// <summary>
            /// Unfortunately we can only know a ref local is returnable after binding the initializer.
            /// </summary>
            private bool _returnable;

            public LocalWithInitializer(
                Symbol containingSymbol,
                Binder binder,
                RefKind refKind,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                EqualsValueClauseSyntax initializer,
                LocalDeclarationKind declarationKind) :
                    base(containingSymbol, binder, refKind, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
                Debug.Assert(initializer != null);

                _initializer = initializer;

                // byval locals are always returnable
                // byref locals with initializers are assumed not returnable unless proven otherwise
                // NOTE: if we assumed returnable, then self-referring initializer could result in 
                //       a randomly changing returnability when initializer is bound concurrently.
                _returnable = refKind == RefKind.None;
            }

            protected override TypeSymbol InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                var newBinder = new ImplicitlyTypedLocalBinder(this.binder, this);
                var initializerOpt = newBinder.BindInferredVariableInitializer(diagnostics, RefKind, _initializer, _initializer);
                if (initializerOpt != null)
                {
                    return initializerOpt.Type;
                }

                return null;
            }

            /// <summary>
            /// Determine the constant value of this local and the corresponding diagnostics.
            /// Set both to constantTuple in a single operation for thread safety.
            /// </summary>
            /// <param name="inProgress">Null for the initial call, non-null if we are in the process of evaluating a constant.</param>
            /// <param name="boundInitValue">If we already have the bound node for the initial value, pass it in to avoid recomputing it.</param>
            private void MakeConstantTuple(LocalSymbol inProgress, BoundExpression boundInitValue)
            {
                if (this.IsConst && _constantTuple == null)
                {
                    var value = Microsoft.CodeAnalysis.ConstantValue.Bad;
                    var initValueNodeLocation = _initializer.Value.Location;
                    var diagnostics = DiagnosticBag.GetInstance();
                    Debug.Assert(inProgress != this);
                    var type = this.Type;
                    if (boundInitValue == null)
                    {
                        var inProgressBinder = new LocalInProgressBinder(this, this.binder);
                        boundInitValue = inProgressBinder.BindVariableOrAutoPropInitializer(_initializer, this.RefKind, type, diagnostics);
                    }

                    value = ConstantValueUtils.GetAndValidateConstantValue(boundInitValue, this, type, initValueNodeLocation, diagnostics);
                    Interlocked.CompareExchange(ref _constantTuple, new EvaluatedConstant(value, diagnostics.ToReadOnlyAndFree()), null);
                }
            }

            internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics = null)
            {
                if (this.IsConst && inProgress == this)
                {
                    if (diagnostics != null)
                    {
                        diagnostics.Add(ErrorCode.ERR_CircConstValue, node.GetLocation(), this);
                    }

                    return Microsoft.CodeAnalysis.ConstantValue.Bad;
                }

                MakeConstantTuple(inProgress, boundInitValue: null);
                return _constantTuple == null ? null : _constantTuple.Value;
            }

            internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
            {
                Debug.Assert(boundInitValue != null);
                MakeConstantTuple(inProgress: null, boundInitValue: boundInitValue);
                return _constantTuple == null ? ImmutableArray<Diagnostic>.Empty : _constantTuple.Diagnostics;
            }

            internal override void SetReturnable()
            {
                _returnable = true;
            }

            internal override bool IsReturnable
            {
                get
                {
                    return _returnable;
                }
            }
        }

        private sealed class ForEachLocal : SourceLocalSymbol
        {
            private readonly ExpressionSyntax _collection;

            public ForEachLocal(
                Symbol containingSymbol,
                Binder binder,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                ExpressionSyntax collection,
                LocalDeclarationKind declarationKind) :
                    base(containingSymbol, binder, RefKind.None, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(declarationKind == LocalDeclarationKind.ForEachIterationVariable);
                _collection = collection;
            }

            protected override TypeSymbol InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                // Normally, it would not be safe to cast to a specific binder type.  However, we verified the type
                // in the factory method call for this symbol.
                return ((ForEachLoopBinder)this.binder).InferCollectionElementType(diagnostics, _collection);
            }
        }
    }
}

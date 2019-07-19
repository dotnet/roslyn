// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private readonly Binder _scopeBinder;

        /// <summary>
        /// Might not be a method symbol.
        /// </summary>
        private readonly Symbol _containingSymbol;

        private readonly SyntaxToken _identifierToken;
        private readonly ImmutableArray<Location> _locations;
        private readonly RefKind _refKind;
        private readonly TypeSyntax _typeSyntax;
        private readonly LocalDeclarationKind _declarationKind;
        private TypeWithAnnotations.Boxed _type;

        /// <summary>
        /// Scope to which the local can "escape" via aliasing/ref assignment.
        /// Not readonly because we can only know escape values after binding the initializer.
        /// </summary>
        protected uint _refEscapeScope;

        /// <summary>
        /// Scope to which the local's values can "escape" via ordinary assignments.
        /// Not readonly because we can only know escape values after binding the initializer.
        /// </summary>
        protected uint _valEscapeScope;

        private SourceLocalSymbol(
            Symbol containingSymbol,
            Binder scopeBinder,
            bool allowRefKind,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind declarationKind)
        {
            Debug.Assert(identifierToken.Kind() != SyntaxKind.None);
            Debug.Assert(declarationKind != LocalDeclarationKind.None);
            Debug.Assert(scopeBinder != null);

            this._scopeBinder = scopeBinder;
            this._containingSymbol = containingSymbol;
            this._identifierToken = identifierToken;
            this._typeSyntax = allowRefKind ? typeSyntax?.SkipRef(out this._refKind) : typeSyntax;
            this._declarationKind = declarationKind;

            // create this eagerly as it will always be needed for the EnsureSingleDefinition
            _locations = ImmutableArray.Create<Location>(identifierToken.GetLocation());

            _refEscapeScope = this._refKind == RefKind.None ?
                                        scopeBinder.LocalScopeDepth :
                                        Binder.ExternalScope; // default to returnable, unless there is initializer

            // we do not know the type yet. 
            // assume this is returnable in case we never get to know our type.
            _valEscapeScope = Binder.ExternalScope;
        }

        /// <summary>
        /// Binder that owns the scope for the local, the one that returns it in its <see cref="Binder.Locals"/> array.
        /// </summary>
        internal Binder ScopeBinder
        {
            get { return _scopeBinder; }
        }

        internal override SyntaxNode ScopeDesignatorOpt
        {
            get { return _scopeBinder.ScopeDesignator; }
        }

        internal override uint RefEscapeScope => _refEscapeScope;

        internal override uint ValEscapeScope => _valEscapeScope;

        /// <summary>
        /// Binder that should be used to bind type syntax for the local.
        /// </summary>
        internal Binder TypeSyntaxBinder
        {
            get { return _scopeBinder; } // Scope binder should be good enough for this.
        }

        // When the variable's type has not yet been inferred,
        // don't let the debugger force inference.
        internal override string GetDebuggerDisplay()
        {
            return _type != null
                ? base.GetDebuggerDisplay()
                : $"{this.Kind} <var> ${this.Name}";
        }

        public static SourceLocalSymbol MakeForeachLocal(
            MethodSymbol containingMethod,
            ForEachLoopBinder binder,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            ExpressionSyntax collection)
        {
            return new ForEachLocalSymbol(containingMethod, binder, typeSyntax, identifierToken, collection, LocalDeclarationKind.ForEachIterationVariable);
        }

        /// <summary>
        /// Make a local variable symbol for an element of a deconstruction,
        /// which can be inferred (if necessary) by binding the enclosing statement.
        /// </summary>
        /// <param name="containingSymbol"></param>
        /// <param name="scopeBinder">
        /// Binder that owns the scope for the local, the one that returns it in its <see cref="Binder.Locals"/> array.
        /// </param>
        /// <param name="nodeBinder">
        /// Enclosing binder for the location where the local is declared.
        /// It should be used to bind something at that location.
        /// </param>
        /// <param name="closestTypeSyntax"></param>
        /// <param name="identifierToken"></param>
        /// <param name="kind"></param>
        /// <param name="deconstruction"></param>
        /// <returns></returns>
        public static SourceLocalSymbol MakeDeconstructionLocal(
            Symbol containingSymbol,
            Binder scopeBinder,
            Binder nodeBinder,
            TypeSyntax closestTypeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind kind,
            SyntaxNode deconstruction)
        {
            Debug.Assert(closestTypeSyntax != null);
            Debug.Assert(nodeBinder != null);

            Debug.Assert(closestTypeSyntax.Kind() != SyntaxKind.RefType);
            return closestTypeSyntax.IsVar
                ? new DeconstructionLocalSymbol(containingSymbol, scopeBinder, nodeBinder, closestTypeSyntax, identifierToken, kind, deconstruction)
                : new SourceLocalSymbol(containingSymbol, scopeBinder, false, closestTypeSyntax, identifierToken, kind);
        }

        /// <summary>
        /// Make a local variable symbol whose type can be inferred (if necessary) by binding and enclosing construct.
        /// </summary>
        internal static LocalSymbol MakeLocalSymbolWithEnclosingContext(
            Symbol containingSymbol,
            Binder scopeBinder,
            Binder nodeBinder,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind kind,
            SyntaxNode nodeToBind,
            SyntaxNode forbiddenZone)
        {
            Debug.Assert(
                nodeToBind.Kind() == SyntaxKind.CasePatternSwitchLabel ||
                nodeToBind.Kind() == SyntaxKind.ThisConstructorInitializer ||
                nodeToBind.Kind() == SyntaxKind.BaseConstructorInitializer ||
                nodeToBind.Kind() == SyntaxKind.SwitchExpressionArm ||
                nodeToBind.Kind() == SyntaxKind.ArgumentList && nodeToBind.Parent is ConstructorInitializerSyntax ||
                nodeToBind.Kind() == SyntaxKind.VariableDeclarator &&
                    new[] { SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement, SyntaxKind.UsingStatement, SyntaxKind.FixedStatement }.
                        Contains(nodeToBind.Ancestors().OfType<StatementSyntax>().First().Kind()) ||
                nodeToBind is ExpressionSyntax);
            Debug.Assert(!(nodeToBind.Kind() == SyntaxKind.SwitchExpressionArm) || nodeBinder is SwitchExpressionArmBinder);
            return typeSyntax?.IsVar != false && kind != LocalDeclarationKind.DeclarationExpressionVariable
                ? new LocalSymbolWithEnclosingContext(containingSymbol, scopeBinder, nodeBinder, typeSyntax, identifierToken, kind, nodeToBind, forbiddenZone)
                : new SourceLocalSymbol(containingSymbol, scopeBinder, false, typeSyntax, identifierToken, kind);
        }

        /// <summary>
        /// Make a local variable symbol which can be inferred (if necessary) by binding its initializing expression.
        /// </summary>
        /// <param name="containingSymbol"></param>
        /// <param name="scopeBinder">
        /// Binder that owns the scope for the local, the one that returns it in its <see cref="Binder.Locals"/> array.
        /// </param>
        /// <param name="allowRefKind"></param>
        /// <param name="typeSyntax"></param>
        /// <param name="identifierToken"></param>
        /// <param name="declarationKind"></param>
        /// <param name="initializer"></param>
        /// <param name="initializerBinderOpt">
        /// Binder that should be used to bind initializer, if different from the <paramref name="scopeBinder"/>.
        /// </param>
        /// <returns></returns>
        public static SourceLocalSymbol MakeLocal(
            Symbol containingSymbol,
            Binder scopeBinder,
            bool allowRefKind,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind declarationKind,
            EqualsValueClauseSyntax initializer = null,
            Binder initializerBinderOpt = null)
        {
            Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
            return (initializer != null)
                ? new LocalWithInitializer(containingSymbol, scopeBinder, typeSyntax, identifierToken, initializer, initializerBinderOpt ?? scopeBinder, declarationKind)
                : new SourceLocalSymbol(containingSymbol, scopeBinder, allowRefKind, typeSyntax, identifierToken, declarationKind);
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
                // even when dealing with "fixed" locals it is the underlying managed reference that gets pinned
                // the pointer variable itself is not pinned.
                return false;
            }
        }

        internal virtual void SetRefEscape(uint value)
        {
            _refEscapeScope = value;
        }

        internal virtual void SetValEscape(uint value)
        {
            _valEscapeScope = value;
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

#if DEBUG
        // We use this to detect infinite recursion in type inference.
        private int concurrentTypeResolutions = 0;
#endif

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                if (_type == null)
                {
#if DEBUG
                    concurrentTypeResolutions++;
                    Debug.Assert(concurrentTypeResolutions < 50);
#endif
                    TypeWithAnnotations localType = GetTypeSymbol();
                    SetTypeWithAnnotations(localType);
                }

                return _type.Value;
            }
        }

        public bool IsVar
        {
            get
            {
                if (_typeSyntax == null)
                {
                    // in "e is {} x" there is no syntax corresponding to the type.
                    return true;
                }

                if (_typeSyntax.IsVar)
                {
                    bool isVar;
                    TypeWithAnnotations declType = this.TypeSyntaxBinder.BindTypeOrVarKeyword(_typeSyntax, new DiagnosticBag(), out isVar);
                    return isVar;
                }

                return false;
            }
        }

        private TypeWithAnnotations GetTypeSymbol()
        {
            var diagnostics = DiagnosticBag.GetInstance();

            Binder typeBinder = this.TypeSyntaxBinder;

            bool isVar;
            TypeWithAnnotations declType;
            if (_typeSyntax == null) // In recursive patterns the type may be omitted.
            {
                isVar = true;
                declType = default;
            }
            else
            {
                declType = typeBinder.BindTypeOrVarKeyword(_typeSyntax.SkipRef(out _), diagnostics, out isVar);
            }

            if (isVar)
            {
                var inferredType = InferTypeOfVarVariable(diagnostics);

                // If we got a valid result that was not void then use the inferred type
                // else create an error type.
                if (inferredType.HasType &&
                    !inferredType.IsVoidType())
                {
                    declType = inferredType;
                }
                else
                {
                    declType = TypeWithAnnotations.Create(typeBinder.CreateErrorType("var"));
                }
            }

            Debug.Assert(declType.HasType);

            //
            // Note that we drop the diagnostics on the floor! That is because this code is invoked mainly in
            // IDE scenarios where we are attempting to use the types of a variable before we have processed
            // the code which causes the variable's type to be inferred. In batch compilation, on the
            // other hand, local variables have their type inferred, if necessary, in the course of binding
            // the statements of a method from top to bottom, and an inferred type is given to a variable
            // before the variable's type is used by the compiler.
            //
            diagnostics.Free();
            return declType;
        }

        protected virtual TypeWithAnnotations InferTypeOfVarVariable(DiagnosticBag diagnostics)
        {
            // TODO: this method must be overridden for pattern variables to bind the
            // expression or statement that is the nearest enclosing to the pattern variable's
            // declaration. That will cause the type of the pattern variable to be set as a side-effect.
            return _type?.Value ?? default;
        }

        internal void SetTypeWithAnnotations(TypeWithAnnotations newType)
        {
            Debug.Assert(!(newType.Type is null));
            TypeSymbol originalType = _type?.Value.DefaultType;

            // In the event that we race to set the type of a local, we should
            // always deduce the same type, or deduce that the type is an error.

            Debug.Assert((object)originalType == null ||
                originalType.IsErrorType() && newType.Type.IsErrorType() ||
                TypeSymbol.Equals(originalType, newType.Type, TypeCompareKind.ConsiderEverything2));

            if ((object)originalType == null)
            {
                Interlocked.CompareExchange(ref _type, new TypeWithAnnotations.Boxed(newType), null);
            }
        }

        /// <summary>
        /// Gets a new local symbol with the given TypeWithAnnotations as the new type. This
        /// type should be identical to the original except for nullability.
        /// </summary>
        internal LocalSymbol WithAnalyzedType(TypeWithAnnotations analyzedType)
        {
            Debug.Assert(analyzedType.Equals(TypeWithAnnotations, TypeCompareKind.AllNullableIgnoreOptions | TypeCompareKind.IgnoreTupleNames));
            if (analyzedType.Equals(TypeWithAnnotations, TypeCompareKind.ConsiderEverything))
            {
                return this;
            }

            var cloned = CloneWithoutType();
            cloned.SetTypeWithAnnotations(analyzedType);
            return cloned;
        }

        protected virtual SourceLocalSymbol CloneWithoutType()
        {
            return new SourceLocalSymbol(_containingSymbol, _scopeBinder, allowRefKind: false, _typeSyntax, _identifierToken, _declarationKind);
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
                        Debug.Assert(node is VariableDeclaratorSyntax);
                        break;

                    case LocalDeclarationKind.Constant:
                    case LocalDeclarationKind.FixedVariable:
                    case LocalDeclarationKind.UsingVariable:
                        Debug.Assert(node is VariableDeclaratorSyntax);
                        break;

                    case LocalDeclarationKind.ForEachIterationVariable:
                        Debug.Assert(node is ForEachStatementSyntax || node is SingleVariableDesignationSyntax);
                        break;

                    case LocalDeclarationKind.CatchVariable:
                        Debug.Assert(node is CatchDeclarationSyntax);
                        break;

                    case LocalDeclarationKind.OutVariable:
                    case LocalDeclarationKind.DeclarationExpressionVariable:
                    case LocalDeclarationKind.DeconstructionVariable:
                    case LocalDeclarationKind.PatternVariable:
                        Debug.Assert(node is SingleVariableDesignationSyntax);
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

        public override RefKind RefKind
        {
            get { return _refKind; }
        }

        public sealed override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if (obj == (object)this)
            {
                return true;
            }

            var symbol = obj as SourceLocalSymbol;
            return (object)symbol != null
                && symbol._identifierToken.Equals(_identifierToken)
                && symbol._containingSymbol.Equals(_containingSymbol, compareKind);
        }

        public sealed override int GetHashCode()
        {
            return Hash.Combine(_identifierToken.GetHashCode(), _containingSymbol.GetHashCode());
        }

        /// <summary>
        /// Symbol for a local whose type can be inferred by binding its initializer.
        /// </summary>
        private sealed class LocalWithInitializer : SourceLocalSymbol
        {
            private readonly EqualsValueClauseSyntax _initializer;
            private readonly Binder _initializerBinder;

            /// <summary>
            /// Store the constant value and the corresponding diagnostics together
            /// to avoid having the former set by one thread and the latter set by
            /// another.
            /// </summary>
            private EvaluatedConstant _constantTuple;

            public LocalWithInitializer(
                Symbol containingSymbol,
                Binder scopeBinder,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                EqualsValueClauseSyntax initializer,
                Binder initializerBinder,
                LocalDeclarationKind declarationKind) :
                    base(containingSymbol, scopeBinder, true, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
                Debug.Assert(initializer != null);

                _initializer = initializer;
                _initializerBinder = initializerBinder;

                // default to the current scope in case we need to handle self-referential error cases.
                _refEscapeScope = _scopeBinder.LocalScopeDepth;
                _valEscapeScope = _scopeBinder.LocalScopeDepth;
            }

            protected override TypeWithAnnotations InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                BoundExpression initializerOpt = this._initializerBinder.BindInferredVariableInitializer(diagnostics, RefKind, _initializer, _initializer);
                return TypeWithAnnotations.Create(initializerOpt?.Type);
            }

            internal override SyntaxNode ForbiddenZone => _initializer;

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
                    Location initValueNodeLocation = _initializer.Value.Location;
                    var diagnostics = DiagnosticBag.GetInstance();
                    Debug.Assert(inProgress != this);
                    var type = this.Type;
                    if (boundInitValue == null)
                    {
                        var inProgressBinder = new LocalInProgressBinder(this, this._initializerBinder);
                        boundInitValue = inProgressBinder.BindVariableOrAutoPropInitializerValue(_initializer, this.RefKind, type, diagnostics);
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

            internal override void SetRefEscape(uint value)
            {
                Debug.Assert(value <= _refEscapeScope);
                _refEscapeScope = value;
            }

            internal override void SetValEscape(uint value)
            {
                Debug.Assert(value <= _valEscapeScope);
                _valEscapeScope = value;
            }

            protected override SourceLocalSymbol CloneWithoutType()
            {
                return new LocalWithInitializer(_containingSymbol, _scopeBinder, _typeSyntax, _identifierToken, _initializer, _initializerBinder, _declarationKind);
            }
        }

        /// <summary>
        /// Symbol for a foreach iteration variable that can be inferred by binding the
        /// collection element type of the foreach.
        /// </summary>
        private sealed class ForEachLocalSymbol : SourceLocalSymbol
        {
            private readonly ExpressionSyntax _collection;

            public ForEachLocalSymbol(
                Symbol containingSymbol,
                ForEachLoopBinder scopeBinder,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                ExpressionSyntax collection,
                LocalDeclarationKind declarationKind) :
                    base(containingSymbol, scopeBinder, allowRefKind: true, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(declarationKind == LocalDeclarationKind.ForEachIterationVariable);
                _collection = collection;
            }

            /// <summary>
            /// We initialize the base's ScopeBinder with a ForEachLoopBinder, so it is safe
            /// to cast it to that type here.
            /// </summary>
            private ForEachLoopBinder ForEachLoopBinder => (ForEachLoopBinder)ScopeBinder;

            protected override TypeWithAnnotations InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                return ForEachLoopBinder.InferCollectionElementType(diagnostics, _collection);
            }

            /// <summary>
            /// There is no forbidden zone for a foreach loop, because the iteration
            /// variable is not in scope in the collection expression.
            /// </summary>
            internal override SyntaxNode ForbiddenZone => null;

            protected override SourceLocalSymbol CloneWithoutType()
            {
                return new ForEachLocalSymbol(_containingSymbol, (ForEachLoopBinder)_scopeBinder, _typeSyntax, _identifierToken, _collection, _declarationKind);
            }
        }

        /// <summary>
        /// Symbol for a deconstruction local that might require type inference.
        /// For instance, local <c>x</c> in <c>var (x, y) = ...</c> or <c>(var x, int y) = ...</c>.
        /// </summary>
        private class DeconstructionLocalSymbol : SourceLocalSymbol
        {
            private readonly SyntaxNode _deconstruction;
            private readonly Binder _nodeBinder;

            public DeconstructionLocalSymbol(
                Symbol containingSymbol,
                Binder scopeBinder,
                Binder nodeBinder,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                LocalDeclarationKind declarationKind,
                SyntaxNode deconstruction)
            : base(containingSymbol, scopeBinder, false, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(deconstruction.Kind() == SyntaxKind.SimpleAssignmentExpression || deconstruction.Kind() == SyntaxKind.ForEachVariableStatement);

                _deconstruction = deconstruction;
                _nodeBinder = nodeBinder;
            }

            protected override TypeWithAnnotations InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                // Try binding enclosing deconstruction-declaration (the top-level VariableDeclaration), this should force the inference.
                switch (_deconstruction.Kind())
                {
                    case SyntaxKind.SimpleAssignmentExpression:
                        var assignment = (AssignmentExpressionSyntax)_deconstruction;
                        Debug.Assert(assignment.IsDeconstruction());
                        DeclarationExpressionSyntax declaration = null;
                        ExpressionSyntax expression = null;
                        _nodeBinder.BindDeconstruction(assignment, assignment.Left, assignment.Right, diagnostics, ref declaration, ref expression);
                        break;

                    case SyntaxKind.ForEachVariableStatement:
                        Debug.Assert(this.ScopeBinder.GetBinder((ForEachVariableStatementSyntax)_deconstruction) == _nodeBinder);
                        _nodeBinder.BindForEachDeconstruction(diagnostics, _nodeBinder);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_deconstruction.Kind());
                }

                return _type.Value;
            }

            internal override SyntaxNode ForbiddenZone
            {
                get
                {
                    switch (_deconstruction.Kind())
                    {
                        case SyntaxKind.SimpleAssignmentExpression:
                            return _deconstruction;

                        case SyntaxKind.ForEachVariableStatement:
                            // There is no forbidden zone for a foreach statement, because the
                            // variables are not in scope in the expression.
                            return null;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(_deconstruction.Kind());
                    }
                }
            }

            protected override SourceLocalSymbol CloneWithoutType()
            {
                return new DeconstructionLocalSymbol(_containingSymbol, _scopeBinder, _nodeBinder, _typeSyntax, _identifierToken, _declarationKind, _deconstruction);
            }
        }

        private class LocalSymbolWithEnclosingContext : SourceLocalSymbol
        {
            private readonly SyntaxNode _forbiddenZone;
            private readonly Binder _nodeBinder;
            private readonly SyntaxNode _nodeToBind;

            public LocalSymbolWithEnclosingContext(
                Symbol containingSymbol,
                Binder scopeBinder,
                Binder nodeBinder,
                TypeSyntax typeSyntax,
                SyntaxToken identifierToken,
                LocalDeclarationKind declarationKind,
                SyntaxNode nodeToBind,
                SyntaxNode forbiddenZone)
                : base(containingSymbol, scopeBinder, false, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(
                    nodeToBind.Kind() == SyntaxKind.CasePatternSwitchLabel ||
                    nodeToBind.Kind() == SyntaxKind.ThisConstructorInitializer ||
                    nodeToBind.Kind() == SyntaxKind.BaseConstructorInitializer ||
                    nodeToBind.Kind() == SyntaxKind.ArgumentList && nodeToBind.Parent is ConstructorInitializerSyntax ||
                    nodeToBind.Kind() == SyntaxKind.VariableDeclarator ||
                    nodeToBind.Kind() == SyntaxKind.SwitchExpressionArm ||
                    nodeToBind is ExpressionSyntax);
                Debug.Assert(!(nodeToBind.Kind() == SyntaxKind.SwitchExpressionArm) || nodeBinder is SwitchExpressionArmBinder);
                this._nodeBinder = nodeBinder;
                this._nodeToBind = nodeToBind;
                this._forbiddenZone = forbiddenZone;
            }

            internal override SyntaxNode ForbiddenZone => _forbiddenZone;

            // This type is currently used for out variables and pattern variables.
            // Pattern variables do not have a forbidden zone, so we only need to produce
            // the diagnostic for out variables here.
            internal override ErrorCode ForbiddenDiagnostic => ErrorCode.ERR_ImplicitlyTypedOutVariableUsedInTheSameArgumentList;

            protected override TypeWithAnnotations InferTypeOfVarVariable(DiagnosticBag diagnostics)
            {
                switch (_nodeToBind.Kind())
                {
                    case SyntaxKind.ThisConstructorInitializer:
                    case SyntaxKind.BaseConstructorInitializer:
                        var initializer = (ConstructorInitializerSyntax)_nodeToBind;
                        _nodeBinder.BindConstructorInitializer(initializer, diagnostics);
                        break;
                    case SyntaxKind.ArgumentList:
                        var invocation = (ConstructorInitializerSyntax)_nodeToBind.Parent;
                        _nodeBinder.BindConstructorInitializer(invocation, diagnostics);
                        break;
                    case SyntaxKind.CasePatternSwitchLabel:
                        _nodeBinder.BindPatternSwitchLabelForInference((CasePatternSwitchLabelSyntax)_nodeToBind, diagnostics);
                        break;
                    case SyntaxKind.VariableDeclarator:
                        // This occurs, for example, in
                        // int x, y[out var Z, 1 is int I];
                        // for (int x, y[out var Z, 1 is int I]; ;) {}
                        _nodeBinder.BindDeclaratorArguments((VariableDeclaratorSyntax)_nodeToBind, diagnostics);
                        break;
                    case SyntaxKind.SwitchExpressionArm:
                        var arm = (SwitchExpressionArmSyntax)_nodeToBind;
                        var armBinder = (SwitchExpressionArmBinder)_nodeBinder;
                        armBinder.BindSwitchExpressionArm(arm, diagnostics);
                        break;
                    default:
                        _nodeBinder.BindExpression((ExpressionSyntax)_nodeToBind, diagnostics);
                        break;
                }

                if (this._type == null)
                {
                    Debug.Assert(this.DeclarationKind == LocalDeclarationKind.DeclarationExpressionVariable);
                    SetTypeWithAnnotations(TypeWithAnnotations.Create(_nodeBinder.CreateErrorType("var")));
                }

                return _type.Value;
            }

            protected override SourceLocalSymbol CloneWithoutType()
            {
                return new LocalSymbolWithEnclosingContext(_containingSymbol, _scopeBinder, _nodeBinder, _typeSyntax, _identifierToken, _declarationKind, _nodeToBind, _forbiddenZone);
            }
        }
    }
}

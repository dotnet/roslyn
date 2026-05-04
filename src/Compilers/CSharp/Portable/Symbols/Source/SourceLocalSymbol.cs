// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Runtime.CompilerServices;

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
        private readonly TypeSyntax _typeSyntax;
        private readonly RefKind _refKind;
        private readonly LocalDeclarationKind _declarationKind;
        private readonly ScopedKind _scope;

#nullable enable

        private TypeWithAnnotations.Boxed? _type;

        // Please don't use thread local storage widely. This should be one of only a few uses.
        [ThreadStatic] private static PooledHashSet<LocalTypeInferenceInProgressKey>? s_LocalTypeInferenceInProgress;
        private ConcurrentSet<SyntaxNode>? _forbiddenReferences;

        private readonly struct LocalTypeInferenceInProgressKey : IEquatable<LocalTypeInferenceInProgressKey>
        {
            public readonly SourceLocalSymbol Local;
            public readonly SyntaxNode Reference;
            public LocalTypeInferenceInProgressKey(SourceLocalSymbol local, SyntaxNode reference)
            {
                Local = local;
                Reference = reference;
            }

            public bool Equals(LocalTypeInferenceInProgressKey other)
            {
                return Local == (object)other.Local && Reference == other.Reference;
            }

            public override bool Equals(object? obj)
            {
                return obj is LocalTypeInferenceInProgressKey && Equals((LocalTypeInferenceInProgressKey)obj);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(RuntimeHelpers.GetHashCode(Local), Reference.GetHashCode());
            }
        }
#nullable disable

        private SourceLocalSymbol(
            Symbol containingSymbol,
            Binder scopeBinder,
            bool allowRefKind,
            bool allowScoped,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind declarationKind)
        {
            Debug.Assert(identifierToken.Kind() != SyntaxKind.None);
            Debug.Assert(declarationKind != LocalDeclarationKind.None);
            Debug.Assert(scopeBinder != null);
            Debug.Assert(containingSymbol.DeclaringCompilation == scopeBinder.Compilation);

            this._scopeBinder = scopeBinder;
            this._containingSymbol = containingSymbol;
            this._identifierToken = identifierToken;

            this._typeSyntax = typeSyntax;

            bool isScoped;
            typeSyntax = typeSyntax.SkipScoped(out isScoped);
            isScoped = isScoped && allowScoped;

            // Diagnostics for ref-locals is reported by caller in BindDeclarationStatementParts.
            if (allowRefKind)
                typeSyntax.SkipRefInLocalOrReturn(diagnostics: null, out _refKind);

            _scope = _refKind != RefKind.None
                ? isScoped ? ScopedKind.ScopedRef : ScopedKind.None
                : isScoped ? ScopedKind.ScopedValue : ScopedKind.None;

            this._declarationKind = declarationKind;
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

        internal sealed override ScopedKind Scope => _scope;

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

            return closestTypeSyntax.SkipScoped(out _).SkipRef().IsVar
                ? new DeconstructionLocalSymbol(containingSymbol, scopeBinder, nodeBinder, closestTypeSyntax, identifierToken, kind, deconstruction)
                : new SourceLocalSymbol(containingSymbol, scopeBinder, allowRefKind: false, allowScoped: true, closestTypeSyntax, identifierToken, kind);
        }

#nullable enable

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
            SyntaxNode nodeToBind)
        {
            Debug.Assert(
                nodeToBind.Kind() == SyntaxKind.CasePatternSwitchLabel ||
                nodeToBind.Kind() == SyntaxKind.ThisConstructorInitializer ||
                nodeToBind.Kind() == SyntaxKind.BaseConstructorInitializer ||
                nodeToBind.Kind() == SyntaxKind.PrimaryConstructorBaseType || // initializer for a record constructor
                nodeToBind.Kind() == SyntaxKind.SwitchExpressionArm ||
                nodeToBind.Kind() == SyntaxKind.ArgumentList && (nodeToBind.Parent is ConstructorInitializerSyntax || nodeToBind.Parent is PrimaryConstructorBaseTypeSyntax) ||
                nodeToBind.Kind() == SyntaxKind.GotoCaseStatement || // for error recovery
                nodeToBind.Kind() == SyntaxKind.VariableDeclarator &&
                    new[] { SyntaxKind.LocalDeclarationStatement, SyntaxKind.ForStatement, SyntaxKind.UsingStatement, SyntaxKind.FixedStatement }.
                        Contains(nodeToBind.Ancestors().OfType<StatementSyntax>().First().Kind()) ||
                nodeToBind is ExpressionSyntax);
            Debug.Assert(!(nodeToBind.Kind() == SyntaxKind.SwitchExpressionArm) || nodeBinder is SwitchExpressionArmBinder);
            return typeSyntax?.SkipScoped(out _).SkipRef().IsVar != false && kind != LocalDeclarationKind.DeclarationExpressionVariable
                ? new LocalSymbolWithEnclosingContext(containingSymbol, scopeBinder, nodeBinder, typeSyntax, identifierToken, kind, nodeToBind)
                : new SourceLocalSymbol(containingSymbol, scopeBinder, allowRefKind: false, allowScoped: true, typeSyntax, identifierToken, kind);
        }

#nullable disable

        /// <summary>
        /// Make a local variable symbol which can be inferred (if necessary) by binding its initializing expression.
        /// </summary>
        /// <param name="containingSymbol"></param>
        /// <param name="scopeBinder">
        /// Binder that owns the scope for the local, the one that returns it in its <see cref="Binder.Locals"/> array.
        /// </param>
        /// <param name="allowRefKind"></param>
        /// <param name="allowScoped"></param>
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
            bool allowScoped,
            TypeSyntax typeSyntax,
            SyntaxToken identifierToken,
            LocalDeclarationKind declarationKind,
            EqualsValueClauseSyntax initializer,
            Binder initializerBinderOpt = null)
        {
            Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
            return (initializer != null)
                ? new LocalWithInitializer(containingSymbol, scopeBinder, typeSyntax, identifierToken, initializer, initializerBinderOpt ?? scopeBinder, declarationKind, allowScoped)
                : new SourceLocalSymbol(containingSymbol, scopeBinder, allowRefKind: allowRefKind, allowScoped: allowScoped, typeSyntax, identifierToken, declarationKind);
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

        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(
            SynthesizedLocalKind kind, SyntaxNode syntax
#if DEBUG
            ,
            [CallerLineNumber] int createdAtLineNumber = 0,
            [CallerFilePath] string createdAtFilePath = null
#endif
            )
        {
            throw ExceptionUtilities.Unreachable();
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

        internal sealed override bool IsKnownToReferToTempIfReferenceType
        {
            get { return false; }
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
                return GetTypeWithAnnotations(CSharpSyntaxTree.Dummy.GetRoot(), BindingDiagnosticBag.Discarded);
            }
        }

        /// <summary>
        /// The diagnostic code to be reported when an inferred variable is used
        /// in its forbidden zone.
        /// </summary>
        protected virtual ErrorCode ForbiddenDiagnostic => ErrorCode.ERR_VariableUsedBeforeDeclaration;

        public bool IsVar
        {
            get
            {
                if (_typeSyntax == null)
                {
                    // in "e is {} x" there is no syntax corresponding to the type.
                    return true;
                }

                TypeSyntax typeSyntax = _typeSyntax.SkipScoped(out _).SkipRef();

                if (typeSyntax.IsVar)
                {
                    bool isVar;
                    TypeWithAnnotations declType = this.TypeSyntaxBinder.BindTypeOrVarKeyword(typeSyntax, BindingDiagnosticBag.Discarded, out isVar);
                    return isVar;
                }

                return false;
            }
        }

#nullable enable
        public override TypeWithAnnotations GetTypeWithAnnotations(SyntaxNode reference, BindingDiagnosticBag diagnostics)
        {
            if (_forbiddenReferences?.Contains(reference) == true)
            {
                diagnostics.Add(ForbiddenDiagnostic, reference.Location, reference);
                return TypeWithAnnotations.Create(this.DeclaringCompilation.ImplicitlyTypedVariableUsedInForbiddenZoneType);
            }

            if (_type == null)
            {
#if DEBUG
                concurrentTypeResolutions++;
                Debug.Assert(concurrentTypeResolutions < 50);
#endif
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
                    //
                    // Note that we drop the diagnostics on the floor! That is because this code is invoked mainly in
                    // IDE scenarios where we are attempting to use the types of a variable before we have processed
                    // the code which causes the variable's type to be inferred. In batch compilation, on the
                    // other hand, local variables have their type inferred, if necessary, in the course of binding
                    // the statements of a method from top to bottom, and an inferred type is given to a variable
                    // before the variable's type is used by the compiler.
                    //
                    declType = typeBinder.BindTypeOrVarKeyword(_typeSyntax.SkipScoped(out _).SkipRef(), BindingDiagnosticBag.Discarded, out isVar);
                }

                if (isVar)
                {
                    bool free = false;
                    var localTypeInferenceInProgress = s_LocalTypeInferenceInProgress;

                    if (localTypeInferenceInProgress is null)
                    {
                        free = true;
                        localTypeInferenceInProgress = (s_LocalTypeInferenceInProgress = PooledHashSet<LocalTypeInferenceInProgressKey>.GetInstance());
                    }

                    var key = new LocalTypeInferenceInProgressKey(this, reference);

                    if (!localTypeInferenceInProgress.Add(key))
                    {
                        Debug.Assert(!free);
                        Debug.Assert(reference != CSharpSyntaxTree.Dummy.GetRoot());

                        if (_forbiddenReferences is null)
                        {
                            Interlocked.CompareExchange(ref _forbiddenReferences, new ConcurrentSet<SyntaxNode>(), null);
                        }

                        bool added = _forbiddenReferences.Add(reference);
                        Debug.Assert(added); // This assert can fail if there is a race between multiple threads. We can remove it if it becomes a problem, confirming the case.
                        diagnostics.Add(ForbiddenDiagnostic, reference.Location, reference);
                        return TypeWithAnnotations.Create(this.DeclaringCompilation.ImplicitlyTypedVariableUsedInForbiddenZoneType);
                    }

                    TypeWithAnnotations inferredType;

                    try
                    {
                        inferredType = InferTypeOfVarVariable();
                    }
                    finally
                    {
                        Debug.Assert(localTypeInferenceInProgress == s_LocalTypeInferenceInProgress);
                        bool removed = localTypeInferenceInProgress.Remove(key);
                        Debug.Assert(removed);
                        Debug.Assert(free == (localTypeInferenceInProgress.Count == 0));

                        if (free)
                        {
                            s_LocalTypeInferenceInProgress = null;
                            localTypeInferenceInProgress.Free();
                        }
                    }

                    if (_forbiddenReferences?.Contains(reference) == true)
                    {
                        diagnostics.Add(ForbiddenDiagnostic, reference.Location, reference);
                        return TypeWithAnnotations.Create(this.DeclaringCompilation.ImplicitlyTypedVariableUsedInForbiddenZoneType);
                    }

                    // If we got a valid result that was not void then use the inferred type
                    // else create an error type.
                    if (inferredType.HasType &&
                        !inferredType.IsVoidType())
                    {
                        declType = inferredType;
                    }
                    else
                    {
                        declType = TypeWithAnnotations.Create(DeclaringCompilation.ImplicitlyTypedVariableInferenceFailedType);
                    }
                }

                Debug.Assert(declType.HasType);
                SetTypeWithAnnotations(declType);
                return _type?.Value ?? declType;
            }

            return _type.Value;
        }
#nullable disable

        protected virtual TypeWithAnnotations InferTypeOfVarVariable()
        {
            // TODO: this method must be overridden for pattern variables to bind the
            // expression or statement that is the nearest enclosing to the pattern variable's
            // declaration. That will cause the type of the pattern variable to be set as a side-effect.
            return _type?.Value ?? default;
        }

        internal void SetTypeWithAnnotations(TypeWithAnnotations newType)
        {
            Debug.Assert(newType.Type is object);
            TypeWithAnnotations? originalType = _type?.Value;

            // In the event that we race to set the type of a local, we should
            // always deduce the same type, or deduce that the type is an error.

            Debug.Assert((object)originalType?.DefaultType == null ||
                originalType.Value.DefaultType.IsErrorType() && newType.Type.IsErrorType() ||
                originalType.Value.TypeSymbolEquals(newType, TypeCompareKind.ConsiderEverything));

            if (_type is null &&
                (newType.Type != (object)DeclaringCompilation.ImplicitlyTypedVariableInferenceFailedType ||
                 (s_LocalTypeInferenceInProgress?.Any(static (key, @this) => key.Local == (object)@this, this) != true)))
            {
                Interlocked.CompareExchange(ref _type, new TypeWithAnnotations.Boxed(newType), null);
            }
        }

        public override Location TryGetFirstLocation()
            => _identifierToken.GetLocation();

        /// <summary>
        /// Gets the locations where the local symbol was originally defined in source.
        /// There should not be local symbols from metadata, and there should be only one local variable declared.
        /// TODO: check if there are multiple same name local variables - error symbol or local symbol?
        /// </summary>
        public override ImmutableArray<Location> Locations
            => ImmutableArray.Create(GetFirstLocation());

        internal sealed override SyntaxNode GetDeclaratorSyntax()
        {
            return _identifierToken.Parent;
        }

        internal override bool HasSourceLocation => true;

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

        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag diagnostics)
        {
            return null;
        }

        internal override ReadOnlyBindingDiagnostic<AssemblySymbol> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return ReadOnlyBindingDiagnostic<AssemblySymbol>.Empty;
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

            // If we're comparing against a symbol that was wrapped and updated for nullable,
            // delegate to its handling of equality, rather than our own.
            if (obj is UpdatedContainingSymbolAndNullableAnnotationLocal updated)
            {
                return updated.Equals(this, compareKind);
            }

            return obj is SourceLocalSymbol symbol
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
                LocalDeclarationKind declarationKind,
                bool allowScoped) :
                    base(containingSymbol, scopeBinder, allowRefKind: true, allowScoped: allowScoped, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(declarationKind != LocalDeclarationKind.ForEachIterationVariable);
                Debug.Assert(initializer != null);

                _initializer = initializer;
                _initializerBinder = initializerBinder;
                if (this.IsConst)
                {
                    _initializerBinder = _initializerBinder.GetBinder(initializer) ?? new LocalInProgressBinder(_initializer, initializerBinder); // for error scenarios
                    recordConstInBinderChain();
                }

                void recordConstInBinderChain()
                {
                    for (var binder = _initializerBinder; binder != null; binder = binder.Next)
                    {
                        if (binder is LocalInProgressBinder localInProgressBinder && localInProgressBinder.InitializerSyntax == _initializer)
                        {
                            localInProgressBinder.SetLocalSymbol(this);
                            return;
                        }
                    }

                    throw ExceptionUtilities.Unreachable();
                }
            }

            protected override TypeWithAnnotations InferTypeOfVarVariable()
            {
                BoundExpression initializerOpt = this._initializerBinder.BindInferredVariableInitializer(BindingDiagnosticBag.Discarded, RefKind, _initializer, _initializer);
                return TypeWithAnnotations.Create(initializerOpt?.Type);
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
                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    Debug.Assert(inProgress != this);
                    var type = this.Type;
                    if (boundInitValue == null)
                    {
                        boundInitValue = this._initializerBinder.BindVariableOrAutoPropInitializerValue(_initializer, this.RefKind, type, diagnostics);
                    }

                    value = ConstantValueUtils.GetAndValidateConstantValue(boundInitValue, this, type, _initializer.Value, diagnostics);
                    Interlocked.CompareExchange(ref _constantTuple, new EvaluatedConstant(value, diagnostics.ToReadOnlyAndFree()), null);
                }
            }

            internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag diagnostics = null)
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

            internal override ReadOnlyBindingDiagnostic<AssemblySymbol> GetConstantValueDiagnostics(BoundExpression boundInitValue)
            {
                Debug.Assert(boundInitValue != null);
                MakeConstantTuple(inProgress: null, boundInitValue: boundInitValue);
                return _constantTuple == null ? ReadOnlyBindingDiagnostic<AssemblySymbol>.Empty : _constantTuple.Diagnostics;
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
                    base(containingSymbol, scopeBinder, allowRefKind: true, allowScoped: true, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(declarationKind == LocalDeclarationKind.ForEachIterationVariable);
                _collection = collection;
            }

            /// <summary>
            /// We initialize the base's ScopeBinder with a ForEachLoopBinder, so it is safe
            /// to cast it to that type here.
            /// </summary>
            private ForEachLoopBinder ForEachLoopBinder => (ForEachLoopBinder)ScopeBinder;

            protected override TypeWithAnnotations InferTypeOfVarVariable()
            {
                return ForEachLoopBinder.InferCollectionElementType(BindingDiagnosticBag.Discarded, _collection);
            }
        }

        /// <summary>
        /// Symbol for a deconstruction local that might require type inference.
        /// For instance, local <c>x</c> in <c>var (x, y) = ...</c> or <c>(var x, int y) = ...</c>.
        /// </summary>
        private sealed class DeconstructionLocalSymbol : SourceLocalSymbol
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
            : base(containingSymbol, scopeBinder, allowRefKind: false, allowScoped: true, typeSyntax, identifierToken, declarationKind)
            {
                _deconstruction = deconstruction;
                _nodeBinder = nodeBinder;
            }

#nullable enable

            protected override TypeWithAnnotations InferTypeOfVarVariable()
            {
                // Try binding enclosing deconstruction-declaration (the top-level VariableDeclaration), this should force the inference.
                switch (_deconstruction.Kind())
                {
                    case SyntaxKind.SimpleAssignmentExpression:
                        var assignment = (AssignmentExpressionSyntax)_deconstruction;
                        Debug.Assert(assignment.IsDeconstruction());
                        DeclarationExpressionSyntax? declaration = null;
                        ExpressionSyntax? expression = null;
                        _nodeBinder.BindDeconstruction(assignment, assignment.Left, assignment.Right, BindingDiagnosticBag.Discarded, ref declaration, ref expression);
                        break;

                    case SyntaxKind.ForEachVariableStatement:
                        Debug.Assert(this.ScopeBinder.GetBinder((ForEachVariableStatementSyntax)_deconstruction) == _nodeBinder);
                        _nodeBinder.BindForEachDeconstruction(BindingDiagnosticBag.Discarded, _nodeBinder);
                        break;

                    default:
                        return TypeWithAnnotations.Create(_nodeBinder.CreateErrorType());
                }

                return _type?.Value ?? default;
            }
        }

        private sealed class LocalSymbolWithEnclosingContext : SourceLocalSymbol
        {
            private readonly Binder _nodeBinder;
            private readonly SyntaxNode _nodeToBind;

            public LocalSymbolWithEnclosingContext(
                Symbol containingSymbol,
                Binder scopeBinder,
                Binder nodeBinder,
                TypeSyntax? typeSyntax,
                SyntaxToken identifierToken,
                LocalDeclarationKind declarationKind,
                SyntaxNode nodeToBind)
                : base(containingSymbol, scopeBinder, allowRefKind: false, allowScoped: true, typeSyntax, identifierToken, declarationKind)
            {
                Debug.Assert(declarationKind is LocalDeclarationKind.OutVariable or LocalDeclarationKind.PatternVariable);
                Debug.Assert(
                    nodeToBind.Kind() == SyntaxKind.CasePatternSwitchLabel ||
                    nodeToBind.Kind() == SyntaxKind.ThisConstructorInitializer ||
                    nodeToBind.Kind() == SyntaxKind.BaseConstructorInitializer ||
                    nodeToBind.Kind() == SyntaxKind.PrimaryConstructorBaseType || // initializer for a record constructor
                    nodeToBind.Kind() == SyntaxKind.ArgumentList && (nodeToBind.Parent is ConstructorInitializerSyntax || nodeToBind.Parent is PrimaryConstructorBaseTypeSyntax) ||
                    nodeToBind.Kind() == SyntaxKind.VariableDeclarator ||
                    nodeToBind.Kind() == SyntaxKind.SwitchExpressionArm ||
                    nodeToBind.Kind() == SyntaxKind.GotoCaseStatement ||
                    nodeToBind is ExpressionSyntax);
                Debug.Assert(!(nodeToBind.Kind() == SyntaxKind.SwitchExpressionArm) || nodeBinder is SwitchExpressionArmBinder);
                this._nodeBinder = nodeBinder;
                this._nodeToBind = nodeToBind;
            }

            // This type is currently used for out variables and pattern variables.
            // Pattern variables do not have a forbidden zone, so we only need to produce
            // the diagnostic for out variables here.
            protected override ErrorCode ForbiddenDiagnostic => ErrorCode.ERR_ImplicitlyTypedVariableUsedInForbiddenZone;

            protected override TypeWithAnnotations InferTypeOfVarVariable()
            {
                switch (_nodeToBind.Kind())
                {
                    case SyntaxKind.ThisConstructorInitializer:
                    case SyntaxKind.BaseConstructorInitializer:
                        var initializer = (ConstructorInitializerSyntax)_nodeToBind;
                        _nodeBinder.BindConstructorInitializer(initializer, BindingDiagnosticBag.Discarded);
                        break;
                    case SyntaxKind.PrimaryConstructorBaseType:
                        _nodeBinder.BindConstructorInitializer((PrimaryConstructorBaseTypeSyntax)_nodeToBind, BindingDiagnosticBag.Discarded);
                        break;
                    case SyntaxKind.ArgumentList:
                        switch (_nodeToBind.Parent)
                        {
                            case ConstructorInitializerSyntax ctorInitializer:
                                _nodeBinder.BindConstructorInitializer(ctorInitializer, BindingDiagnosticBag.Discarded);
                                break;
                            case PrimaryConstructorBaseTypeSyntax ctorInitializer:
                                _nodeBinder.BindConstructorInitializer(ctorInitializer, BindingDiagnosticBag.Discarded);
                                break;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(_nodeToBind.Parent);
                        }
                        break;
                    case SyntaxKind.CasePatternSwitchLabel:
                        _nodeBinder.BindPatternSwitchLabelForInference((CasePatternSwitchLabelSyntax)_nodeToBind, BindingDiagnosticBag.Discarded);
                        break;
                    case SyntaxKind.VariableDeclarator:
                        // This occurs, for example, in
                        // int x, y[out var Z, 1 is int I];
                        // for (int x, y[out var Z, 1 is int I]; ;) {}
                        _nodeBinder.BindDeclaratorArguments((VariableDeclaratorSyntax)_nodeToBind, BindingDiagnosticBag.Discarded);
                        break;
                    case SyntaxKind.SwitchExpressionArm:
                        var arm = (SwitchExpressionArmSyntax)_nodeToBind;
                        var armBinder = (SwitchExpressionArmBinder)_nodeBinder;
                        armBinder.BindSwitchExpressionArm(arm, BindingDiagnosticBag.Discarded);
                        break;
                    case SyntaxKind.GotoCaseStatement:
                        _nodeBinder.BindStatement((GotoStatementSyntax)_nodeToBind, BindingDiagnosticBag.Discarded);
                        break;
                    default:
                        _nodeBinder.BindExpression((ExpressionSyntax)_nodeToBind, BindingDiagnosticBag.Discarded);
                        break;
                }

                if (this._type == null)
                {
                    Debug.Assert(this.DeclarationKind is LocalDeclarationKind.DeclarationExpressionVariable or LocalDeclarationKind.OutVariable);
                    SetTypeWithAnnotations(TypeWithAnnotations.Create(DeclaringCompilation.ImplicitlyTypedVariableInferenceFailedType));
                }

                return _type?.Value ?? default;
            }
        }
    }
}

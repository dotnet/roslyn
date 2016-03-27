// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Threading;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class LocalFunctionSymbol : MethodSymbol
    {
        private readonly Binder _binder;
        private readonly LocalFunctionStatementSyntax _syntax;
        private readonly Symbol _containingSymbol;
        private readonly DeclarationModifiers _declarationModifiers;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private ImmutableArray<ParameterSymbol> _parameters;
        private ImmutableArray<TypeParameterConstraintClause> _lazyTypeParameterConstraints;
        private readonly RefKind _refKind;
        private TypeSymbolWithAnnotations _returnType;
        private bool _isVararg;
        private TypeSymbol _iteratorElementType;

        // TODO: Find a better way to report diagnostics.
        // We can't put binding in the constructor, as it creates infinite recursion.
        // We can't report to Compilation.DeclarationDiagnostics as it's already too late and they will be dropped.
        // The current system is to dump diagnostics into this field, and then grab them out again in Binder_Statements.BindLocalFunctionStatement
        private ImmutableArray<Diagnostic> _diagnostics;

        public LocalFunctionSymbol(
            Binder binder,
            NamedTypeSymbol containingType,
            Symbol containingSymbol,
            LocalFunctionStatementSyntax syntax)
        {
            _syntax = syntax;
            _containingSymbol = containingSymbol;
            _refKind = syntax.RefKeyword.Kind().GetRefKind();

            _declarationModifiers =
                DeclarationModifiers.Private |
                DeclarationModifiers.Static |
                syntax.Modifiers.ToDeclarationModifiers();

            var diagnostics = DiagnosticBag.GetInstance();

            if (_syntax.TypeParameterList != null)
            {
                binder = new WithMethodTypeParametersBinder(this, binder);
                _typeParameters = MakeTypeParameters(diagnostics);
            }
            else
            {
                _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }

            if (IsExtensionMethod)
            {
                diagnostics.Add(ErrorCode.ERR_BadExtensionAgg, Locations[0]);
            }

            _binder = binder;
            _diagnostics = diagnostics.ToReadOnlyAndFree();
        }

        internal void GrabDiagnostics(DiagnosticBag addTo)
        {
            // force lazy init
            ComputeParameters();
            ComputeReturnType();

            var diags = ImmutableInterlocked.InterlockedExchange(ref _diagnostics, default(ImmutableArray<Diagnostic>));
            if (!diags.IsDefault)
            {
                addTo.AddRange(diags);
            }
        }

        private void AddDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        {
            // Atomic update operation. Applies a function (Concat) to a variable repeatedly, until it "gets through" (isn't in a race condition with another concat)
            var oldDiags = _diagnostics;
            while (true)
            {
                var newDiags = oldDiags.IsDefault ? diagnostics : oldDiags.Concat(diagnostics);
                var overwriteDiags = ImmutableInterlocked.InterlockedCompareExchange(ref _diagnostics, newDiags, oldDiags);
                if (overwriteDiags == oldDiags)
                {
                    break;
                }
                oldDiags = overwriteDiags;
            }
        }

        public override bool IsVararg
        {
            get
            {
                ComputeParameters();
                return _isVararg;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                ComputeParameters();
                return _parameters;
            }
        }

        private void ComputeParameters()
        {
            if (!_parameters.IsDefault)
            {
                return;
            }
            var diagnostics = DiagnosticBag.GetInstance();
            SyntaxToken arglistToken;
            _parameters = ParameterHelpers.MakeParameters(_binder, this, _syntax.ParameterList, true, out arglistToken, diagnostics, true);
            _isVararg = (arglistToken.Kind() == SyntaxKind.ArgListKeyword);
            if (diagnostics.IsEmptyWithoutResolution)
            {
                SourceMemberMethodSymbol.ReportAsyncParameterErrors(this, diagnostics, this.Locations[0]);
            }
            AddDiagnostics(diagnostics.ToReadOnlyAndFree());
        }

        public override TypeSymbolWithAnnotations ReturnType
        {
            get
            {
                return ComputeReturnType();
            }
        }

        internal override RefKind RefKind
        {
            get
            {
                return _refKind;
            }
        }

        internal TypeSymbolWithAnnotations ComputeReturnType()
        {
            if ((object)_returnType != null)
            {
                return _returnType;
            }

            var diagnostics = DiagnosticBag.GetInstance();
            TypeSymbolWithAnnotations returnType = _binder.BindType(_syntax.ReturnType, diagnostics);
            var raceReturnType = Interlocked.CompareExchange(ref _returnType, returnType, null);
            if ((object)raceReturnType != null)
            {
                diagnostics.Free();
                return raceReturnType;
            }
            if (this.IsAsync && !this.IsGenericTaskReturningAsync(_binder.Compilation) && !this.IsTaskReturningAsync(_binder.Compilation) && !this.IsVoidReturningAsync())
            {
                // The return type of an async method must be void, Task or Task<T>
                diagnostics.Add(ErrorCode.ERR_BadAsyncReturn, this.Locations[0]);
            }

            if (this.RefKind != RefKind.None && returnType.SpecialType == SpecialType.System_Void)
            {
                diagnostics.Add(ErrorCode.ERR_VoidReturningMethodCannotReturnByRef, this.Locations[0]);
            }

            // TODO: note there is a race condition here that will ultimately need to be fixed.
            // Specifically, the Interlocked.CompareExchange above succeeds, and will be seen by
            // other threads, before the diagnostics have been recorded in this symbol, below.
            // We can resolve this with a lock around this method and any other methods that
            // manipulate _diagnostics, directly or indirectly.
            AddDiagnostics(diagnostics.ToReadOnlyAndFree());
            return returnType;
        }

        public override bool ReturnsVoid => ReturnType?.SpecialType == SpecialType.System_Void;

        public override int Arity => TypeParameters.Length;

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments => TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations);

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _typeParameters;

        public override bool IsExtensionMethod
        {
            get
            {
                // It is an error to be an extension method, but we need to compute it to report it
                var firstParam = _syntax.ParameterList.Parameters.FirstOrDefault();
                return firstParam != null &&
                    !firstParam.IsArgList &&
                    firstParam.Modifiers.Any(SyntaxKind.ThisKeyword);
            }
        }

        internal override TypeSymbol IteratorElementType
        {
            get
            {
                return _iteratorElementType;
            }
            set
            {
                Debug.Assert((object)_iteratorElementType == null || _iteratorElementType == value);
                Interlocked.CompareExchange(ref _iteratorElementType, value, null);
            }
        }

        public override MethodKind MethodKind => MethodKind.LocalFunction;

        public sealed override Symbol ContainingSymbol => _containingSymbol;

        public override string Name => _syntax.Identifier.ValueText;

        public SyntaxToken NameToken => _syntax.Identifier;

        internal override bool HasSpecialName => false;

        public override bool HidesBaseMethodsByName => false;


        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;


        public override ImmutableArray<Location> Locations => ImmutableArray.Create(_syntax.Identifier.GetLocation());

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray.Create(_syntax.GetReference());


        internal override bool GenerateDebugInfo => true;

        internal override MethodImplAttributes ImplementationAttributes => default(MethodImplAttributes);

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;


        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => null;

        internal override CallingConvention CallingConvention => CallingConvention.Default;

        internal override bool HasDeclarativeSecurity => false;
        internal override bool RequiresSecurityObject => false;

        public override Symbol AssociatedSymbol => null;

        public override Accessibility DeclaredAccessibility => ModifierUtils.EffectiveAccessibility(_declarationModifiers);

        public override bool IsAsync => (_declarationModifiers & DeclarationModifiers.Async) != 0;

        public override bool IsStatic => (_declarationModifiers & DeclarationModifiers.Static) != 0;

        public override bool IsVirtual => (_declarationModifiers & DeclarationModifiers.Virtual) != 0;

        public override bool IsOverride => (_declarationModifiers & DeclarationModifiers.Override) != 0;

        public override bool IsAbstract => (_declarationModifiers & DeclarationModifiers.Abstract) != 0;

        public override bool IsSealed => (_declarationModifiers & DeclarationModifiers.Sealed) != 0;

        public override bool IsExtern => (_declarationModifiers & DeclarationModifiers.Extern) != 0;

        public bool IsUnsafe => (_declarationModifiers & DeclarationModifiers.Unsafe) != 0;

        public override DllImportData GetDllImportData() => null;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            // Local function symbols have no "this" parameter
            thisParameter = null;
            return true;
        }

        private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(DiagnosticBag diagnostics)
        {
            var result = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var typeParameters = _syntax.TypeParameterList.Parameters;
            for (int ordinal = 0; ordinal < typeParameters.Count; ordinal++)
            {
                var parameter = typeParameters[ordinal];
                var identifier = parameter.Identifier;
                var location = identifier.GetLocation();
                var name = identifier.ValueText;

                // TODO: Add diagnostic checks for nested local functions (and containing method)
                if (name == this.Name)
                {
                    diagnostics.Add(ErrorCode.ERR_TypeVariableSameAsParent, location, name);
                }

                for (int i = 0; i < result.Count; i++)
                {
                    if (name == result[i].Name)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                        break;
                    }
                }

                var tpEnclosing = ContainingSymbol.FindEnclosingTypeParameter(name);
                if ((object)tpEnclosing != null)
                {
                    // Type parameter '{0}' has the same name as the type parameter from outer type '{1}'
                    diagnostics.Add(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, location, name, tpEnclosing.ContainingSymbol);
                }

                var typeParameter = new LocalFunctionTypeParameterSymbol(
                        this,
                        name,
                        ordinal,
                        ImmutableArray.Create(location),
                        ImmutableArray.Create(parameter.GetReference()));

                result.Add(typeParameter);
            }

            return result.ToImmutableAndFree();
        }

        internal TypeParameterConstraintKind GetTypeParameterConstraints(int ordinal)
        {
            var clause = this.GetTypeParameterConstraintClause(ordinal);
            return (clause != null) ? clause.Constraints : TypeParameterConstraintKind.None;
        }

        internal ImmutableArray<TypeSymbolWithAnnotations> GetTypeParameterConstraintTypes(int ordinal)
        {
            var clause = this.GetTypeParameterConstraintClause(ordinal);
            return (clause != null) ? clause.ConstraintTypes : ImmutableArray<TypeSymbolWithAnnotations>.Empty;
        }

        private TypeParameterConstraintClause GetTypeParameterConstraintClause(int ordinal)
        {
            if (_lazyTypeParameterConstraints.IsDefault)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                if (ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameterConstraints, MakeTypeParameterConstraints(diagnostics)))
                {
                    AddDiagnostics(diagnostics.ToReadOnly());
                }
                diagnostics.Free();
            }

            var clauses = _lazyTypeParameterConstraints;
            return (clauses.Length > 0) ? clauses[ordinal] : null;
        }

        private ImmutableArray<TypeParameterConstraintClause> MakeTypeParameterConstraints(DiagnosticBag diagnostics)
        {
            var typeParameters = this.TypeParameters;
            if (typeParameters.Length == 0)
            {
                return ImmutableArray<TypeParameterConstraintClause>.Empty;
            }

            var constraintClauses = _syntax.ConstraintClauses;
            if (constraintClauses.Count == 0)
            {
                return ImmutableArray<TypeParameterConstraintClause>.Empty;
            }

            var syntaxTree = _syntax.SyntaxTree;

            // Wrap binder from factory in a generic constraints specific binder
            // to avoid checking constraints when binding type names.
            Debug.Assert(!_binder.Flags.Includes(BinderFlags.GenericConstraintsClause));
            var binder = _binder.WithAdditionalFlags(BinderFlags.GenericConstraintsClause | BinderFlags.SuppressConstraintChecks);

            var result = binder.BindTypeParameterConstraintClauses(this, typeParameters, constraintClauses, diagnostics);
            this.CheckConstraintTypesVisibility(new SourceLocation(_syntax.Identifier), result, diagnostics);
            return result;
        }

        public override int GetHashCode()
        {
            // this is what lambdas do (do not use hashes of other fields)
            return _syntax.GetHashCode();
        }

        public sealed override bool Equals(object symbol)
        {
            if ((object)this == symbol) return true;

            var localFunction = symbol as LocalFunctionSymbol;
            return (object)localFunction != null
                && localFunction._syntax == _syntax;
        }
    }
}

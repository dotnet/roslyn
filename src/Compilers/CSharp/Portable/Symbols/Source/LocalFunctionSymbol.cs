// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LocalFunctionSymbol : MethodSymbol
    {
        private sealed class ParametersAndDiagnostics
        {
            internal readonly ImmutableArray<ParameterSymbol> Parameters;
            internal readonly bool IsVararg;
            internal readonly ImmutableArray<Diagnostic> Diagnostics;

            internal ParametersAndDiagnostics(ImmutableArray<ParameterSymbol> parameters, bool isVararg, ImmutableArray<Diagnostic> diagnostics)
            {
                Parameters = parameters;
                IsVararg = isVararg;
                Diagnostics = diagnostics;
            }
        }

        private sealed class TypeParameterConstraintsAndDiagnostics
        {
            internal readonly ImmutableArray<TypeParameterConstraintClause> ConstraintClauses;
            internal readonly ImmutableArray<Diagnostic> Diagnostics;

            internal TypeParameterConstraintsAndDiagnostics(ImmutableArray<TypeParameterConstraintClause> constraintClauses, ImmutableArray<Diagnostic> diagnostics)
            {
                ConstraintClauses = constraintClauses;
                Diagnostics = diagnostics;
            }
        }

        private sealed class ReturnTypeAndDiagnostics
        {
            internal readonly TypeSymbol ReturnType;
            internal readonly ImmutableArray<Diagnostic> Diagnostics;

            internal ReturnTypeAndDiagnostics(TypeSymbol returnType, ImmutableArray<Diagnostic> diagnostics)
            {
                ReturnType = returnType;
                Diagnostics = diagnostics;
            }
        }

        private readonly Binder _binder;
        private readonly LocalFunctionStatementSyntax _syntax;
        private readonly Symbol _containingSymbol;
        private readonly DeclarationModifiers _declarationModifiers;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly RefKind _refKind;
        private ParametersAndDiagnostics _lazyParametersAndDiagnostics;
        private TypeParameterConstraintsAndDiagnostics _lazyTypeParameterConstraintsAndDiagnostics;
        private ReturnTypeAndDiagnostics _lazyReturnTypeAndDiagnostics;
        private TypeSymbol _iteratorElementType;
        private ImmutableArray<Diagnostic> _diagnostics;

        public LocalFunctionSymbol(
            Binder binder,
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
                addTo.AddRange(_lazyParametersAndDiagnostics.Diagnostics);
                // Note _lazyParametersAndDiagnostics and _lazyReturnTypeAndDiagnostics
                // are computed always, but _lazyTypeParameterConstraintsAndDiagnostics
                // is only computed if there are constraints.
                if (_lazyTypeParameterConstraintsAndDiagnostics != null)
                {
                    addTo.AddRange(_lazyTypeParameterConstraintsAndDiagnostics.Diagnostics);
                }
                addTo.AddRange(_lazyReturnTypeAndDiagnostics.Diagnostics);
            }
        }

        public override bool IsVararg
        {
            get
            {
                ComputeParameters();
                return _lazyParametersAndDiagnostics.IsVararg;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                ComputeParameters();
                return _lazyParametersAndDiagnostics.Parameters;
            }
        }

        private void ComputeParameters()
        {
            if (_lazyParametersAndDiagnostics != null)
            {
                return;
            }

            var diagnostics = DiagnosticBag.GetInstance();
            SyntaxToken arglistToken;
            var parameters = ParameterHelpers.MakeParameters(_binder, this, _syntax.ParameterList, true, out arglistToken, diagnostics, true);
            var isVararg = (arglistToken.Kind() == SyntaxKind.ArgListKeyword);
            if (IsAsync && diagnostics.IsEmptyWithoutResolution)
            {
                SourceMemberMethodSymbol.ReportAsyncParameterErrors(parameters, diagnostics, this.Locations[0]);
            }
            var value = new ParametersAndDiagnostics(parameters, isVararg, diagnostics.ToReadOnlyAndFree());
            Interlocked.CompareExchange(ref _lazyParametersAndDiagnostics, value, null);
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                ComputeReturnType();
                return _lazyReturnTypeAndDiagnostics.ReturnType;
            }
        }

        internal override RefKind RefKind
        {
            get
            {
                return _refKind;
            }
        }

        internal void ComputeReturnType()
        {
            if (_lazyReturnTypeAndDiagnostics != null)
            {
                return;
            }

            var diagnostics = DiagnosticBag.GetInstance();
            TypeSymbol returnType = _binder.BindType(_syntax.ReturnType, diagnostics);
            if (IsAsync &&
                returnType.SpecialType != SpecialType.System_Void &&
                !returnType.IsNonGenericTaskType(_binder.Compilation) &&
                !returnType.IsGenericTaskType(_binder.Compilation))
            {
                // The return type of an async method must be void, Task or Task<T>
                diagnostics.Add(ErrorCode.ERR_BadAsyncReturn, this.Locations[0]);
            }
            if (_refKind != RefKind.None && returnType.SpecialType == SpecialType.System_Void)
            {
                diagnostics.Add(ErrorCode.ERR_VoidReturningMethodCannotReturnByRef, this.Locations[0]);
            }
            var value = new ReturnTypeAndDiagnostics(returnType, diagnostics.ToReadOnlyAndFree());
            Interlocked.CompareExchange(ref _lazyReturnTypeAndDiagnostics, value, null);
        }

        public override bool ReturnsVoid => ReturnType?.SpecialType == SpecialType.System_Void;

        public override int Arity => TypeParameters.Length;

        public override ImmutableArray<TypeSymbol> TypeArguments => TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>();

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

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => ImmutableArray<CustomModifier>.Empty;

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

        internal ImmutableArray<TypeSymbol> GetTypeParameterConstraintTypes(int ordinal)
        {
            var clause = this.GetTypeParameterConstraintClause(ordinal);
            return (clause != null) ? clause.ConstraintTypes : ImmutableArray<TypeSymbol>.Empty;
        }

        private TypeParameterConstraintClause GetTypeParameterConstraintClause(int ordinal)
        {
            if (_lazyTypeParameterConstraintsAndDiagnostics == null)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                var constraints = MakeTypeParameterConstraints(diagnostics);
                var value = new TypeParameterConstraintsAndDiagnostics(constraints, diagnostics.ToReadOnlyAndFree());
                Interlocked.CompareExchange(ref _lazyTypeParameterConstraintsAndDiagnostics, value, null);
            }

            var clauses = _lazyTypeParameterConstraintsAndDiagnostics.ConstraintClauses;
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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class FunctionPointerMethodSymbol : MethodSymbol
    {
        private readonly ImmutableArray<FunctionPointerParameterSymbol> _parameters;
        private DiagnosticInfo? _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo;

        public static FunctionPointerMethodSymbol CreateMethodFromSource(FunctionPointerTypeSyntax syntax, Binder typeBinder, DiagnosticBag diagnostics, ConsList<TypeSymbol> basesBeingResolved, bool suppressUseSiteDiagnostics)
        {
            var (callingConvention, conventionIsValid) = FunctionPointerTypeSymbol.GetCallingConvention(syntax.CallingConvention.Text);
            if (!conventionIsValid)
            {
                // '{0}' is not a valid calling convention for a function pointer. Valid conventions are 'cdecl', 'managed', 'thiscall', and 'stdcall'.
                diagnostics.Add(ErrorCode.ERR_InvalidFunctionPointerCallingConvention, syntax.CallingConvention.GetLocation(), syntax.CallingConvention.Text);
            }

            RefKind refKind = RefKind.None;
            TypeWithAnnotations returnType;

            if (syntax.Parameters.Count == 0)
            {
                returnType = TypeWithAnnotations.Create(typeBinder.CreateErrorType());
            }
            else
            {
                var returnTypeParameter = syntax.Parameters[^1];


                var modifiers = returnTypeParameter.Modifiers;
                for (int i = 0; i < modifiers.Count; i++)
                {
                    var modifier = modifiers[i];
                    switch (modifier.Kind())
                    {
                        case SyntaxKind.RefKeyword when refKind == RefKind.None:
                            if (modifiers.Count > i + 1 && modifiers[i + 1].Kind() == SyntaxKind.ReadOnlyKeyword)
                            {
                                i++;
                                refKind = RefKind.RefReadOnly;
                            }
                            else
                            {
                                refKind = RefKind.Ref;
                            }

                            break;

                        case SyntaxKind.RefKeyword:
                            Debug.Assert(refKind != RefKind.None);
                            // A return type can only have one '{0}' modifier.
                            diagnostics.Add(ErrorCode.ERR_DupReturnTypeMod, modifier.GetLocation(), modifier.Text);
                            break;

                        default:
                            // '{0}' is not a valid function pointer return type modifier. Valid modifiers are 'ref' and 'ref readonly'.
                            diagnostics.Add(ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier, modifier.GetLocation(), modifier.Text);
                            break;
                    }
                }

                returnType = typeBinder.BindType(returnTypeParameter.Type, diagnostics, basesBeingResolved, suppressUseSiteDiagnostics);

                if (returnType.IsVoidType() && refKind != RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_NoVoidHere, returnTypeParameter.GetLocation());
                }
            }

            return new FunctionPointerMethodSymbol(
                callingConvention,
                refKind,
                returnType,
                syntax,
                typeBinder,
                diagnostics,
                suppressUseSiteDiagnostics);
        }

        private FunctionPointerMethodSymbol(
            CallingConvention callingConvention,
            RefKind refKind,
            TypeWithAnnotations returnType,
            FunctionPointerTypeSyntax syntax,
            Binder typeBinder,
            DiagnosticBag diagnostics,
            bool suppressUseSiteDiagnostics)
        {
            CallingConvention = callingConvention;
            RefKind = refKind;
            ReturnTypeWithAnnotations = returnType;

            _parameters = syntax.Parameters.Count > 1
                ? ParameterHelpers.MakeFunctionPointerParameters(
                    typeBinder,
                    this,
                    syntax.Parameters,
                    diagnostics,
                    suppressUseSiteDiagnostics)
                : ImmutableArray<FunctionPointerParameterSymbol>.Empty;
        }

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            if (!(other is FunctionPointerMethodSymbol method))
            {
                return false;
            }

            return Equals(method, compareKind, isValueTypeOverride: null);
        }

        internal bool Equals(FunctionPointerMethodSymbol other, TypeCompareKind compareKind, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverride)
        {
            return EqualsParametersVerified(other, compareKind, isValueTypeOverride)
                && _parameters.SequenceEqual(other._parameters, (compareKind, isValueTypeOverride),
                    (param1, param2, args) => param1.EqualsContainingVerified(param2, args.compareKind, args.isValueTypeOverride));
        }

        private bool EqualsParametersVerified(FunctionPointerMethodSymbol other, TypeCompareKind compareKind, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverride)
            => CallingConvention == other.CallingConvention
               && RefKind == other.RefKind
               && ReturnTypeWithAnnotations.Equals(other.ReturnTypeWithAnnotations, compareKind, isValueTypeOverride);

        public override int GetHashCode()
        {
            var currentHash = GetHashCodeNoParameters();
            foreach (var param in _parameters)
            {
                currentHash = Hash.Combine(param.GetHashCodeNoContaining(), currentHash);
            }
            return currentHash;
        }

        internal int GetHashCodeNoParameters()
            => Hash.Combine(ReturnType, Hash.Combine(CallingConvention.GetHashCode(), RefKind.GetHashCode()));

        internal override CallingConvention CallingConvention { get; }
        public override bool ReturnsVoid => ReturnTypeWithAnnotations.IsVoidType();
        public override RefKind RefKind { get; }
        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }
        public override ImmutableArray<ParameterSymbol> Parameters =>
            _parameters.Cast<FunctionPointerParameterSymbol, ParameterSymbol>();
        // PROTOTYPE(func-ptr): Implement custom modifiers
        public override ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();
        public override MethodKind MethodKind => MethodKind.FunctionPointerSignature;

        internal override DiagnosticInfo? GetUseSiteDiagnostic()
        {
            if (_lazyUseSiteDiagnostic == CSDiagnosticInfo.EmptyErrorInfo)
            {
                DiagnosticInfo? info = null;
                CalculateUseSiteDiagnostic(ref info);
                _lazyUseSiteDiagnostic = info;
            }

            return _lazyUseSiteDiagnostic;
        }

        internal bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo? result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return ReturnType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes)
                || GetUnificationUseSiteDiagnosticRecursive(ref result, RefCustomModifiers, owner, ref checkedTypes)
                || GetUnificationUseSiteDiagnosticRecursive(ref result, Parameters, owner, ref checkedTypes);
        }

        public override bool IsVararg => false; // PROTOTYPE(func-ptr): Varargs

        public override Symbol? ContainingSymbol => null;
        // Function pointers cannot have type parameters
        public override int Arity => 0;
        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;
        public override bool IsExtensionMethod => false;
        public override bool HidesBaseMethodsByName => false;
        public override bool IsAsync => false;
        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;
        public override Symbol? AssociatedSymbol => null;
        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;
        public override bool IsStatic => true;
        public override bool IsVirtual => false;
        public override bool IsOverride => false;
        public override bool IsAbstract => false;
        public override bool IsSealed => true;
        public override bool IsExtern => false;
        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;
        internal override bool HasSpecialName => false;
        internal override MethodImplAttributes ImplementationAttributes => default;
        internal override bool HasDeclarativeSecurity => false;
        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;
        internal override bool RequiresSecurityObject => false;
        internal override bool IsDeclaredReadOnly => false;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => throw ExceptionUtilities.Unreachable;
        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => throw ExceptionUtilities.Unreachable;
        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => throw ExceptionUtilities.Unreachable;
        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => throw ExceptionUtilities.Unreachable;
        internal override bool GenerateDebugInfo => throw ExceptionUtilities.Unreachable;
        internal override ObsoleteAttributeData? ObsoleteAttributeData => throw ExceptionUtilities.Unreachable;
        public override DllImportData GetDllImportData() => throw ExceptionUtilities.Unreachable;
        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw ExceptionUtilities.Unreachable;
        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => throw ExceptionUtilities.Unreachable;
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => throw ExceptionUtilities.Unreachable;
        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => throw ExceptionUtilities.Unreachable;
    }
}

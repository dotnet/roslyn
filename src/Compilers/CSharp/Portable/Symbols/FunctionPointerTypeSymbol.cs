// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class FunctionPointerTypeSymbol : TypeSymbol
    {

        public static FunctionPointerTypeSymbol CreateFunctionPointerTypeSymbolFromSource(FunctionPointerTypeSyntax syntax, Binder typeBinder, DiagnosticBag diagnostics, ConsList<TypeSymbol> basesBeingResolved)
        {
            var symbol = new FunctionPointerTypeSymbol();
            var signature = FunctionPointerMethodSymbol.CreateMethodFromSource(symbol, syntax, typeBinder, diagnostics, basesBeingResolved);

            symbol.Signature = signature;

            return symbol;
        }

        public static CallingConvention GetCallingConvention(string convention) =>
            convention switch
            {
                "" => CallingConvention.Default,
                "cdecl" => CallingConvention.C,
                "managed" => CallingConvention.Default,
                "thiscall" => CallingConvention.ThisCall,
                "stdcall" => CallingConvention.Standard,
                _ => CallingConvention.Invalid,
            };

        private FunctionPointerTypeSymbol()
        {
        }

        public MethodSymbol Signature { get; private set; } = null!;

        public override bool IsReferenceType => false;
        public override bool IsValueType => true;
        public override TypeKind TypeKind => TypeKind.FunctionPointer;
        public override bool IsRefLikeType => false;
        public override bool IsReadOnly => false;
        public override SymbolKind Kind => SymbolKind.FunctionPointer;
        public override Symbol? ContainingSymbol => null;
        public override ImmutableArray<Location> Locations => ImmutableArray<global::Microsoft.CodeAnalysis.Location>.Empty;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<global::Microsoft.CodeAnalysis.SyntaxReference>.Empty;
        public override Accessibility DeclaredAccessibility => Accessibility.NotApplicable;
        public override bool IsStatic => false;
        public override bool IsAbstract => false;
        public override bool IsSealed => false;
        // Pointers do not support boxing, so they really have no base type.
        internal override NamedTypeSymbol? BaseTypeNoUseSiteDiagnostics => null;
        internal override ManagedKind ManagedKind => ManagedKind.Unmanaged;
        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;
        public override void Accept(CSharpSymbolVisitor visitor) => visitor.VisitFunctionPointerType(this);
        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor) => visitor.VisitFunctionPointerType(this);
        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray<Symbol>.Empty;
        public override ImmutableArray<Symbol> GetMembers(string name) => ImmutableArray<Symbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a) => visitor.VisitFunctionPointerType(this, a);

        protected override ISymbol CreateISymbol()
        {
            // PROTOTYPE(func-ptr): Implement
            throw new NotImplementedException();
        }

        protected override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            // PROTOTYPE(func-ptr): Implement
            throw new NotImplementedException();
        }


        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            // PROTOTYPE(func-ptr): Implement
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            // PROTOTYPE(func-ptr): Implement
            result = this;
            return true;
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null)
        {
            throw new NotImplementedException();
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            throw new NotImplementedException();
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            throw new NotImplementedException();
        }

        // PROTOTYPE(func-ptr): Make source and metadata versions
        private sealed class FunctionPointerMethodSymbol : MethodSymbol
        {
            private readonly FunctionPointerTypeSymbol _parent;
            private ImmutableArray<ParameterSymbol> _lazyParameterSymbols = ImmutableArray<ParameterSymbol>.Empty;

            public static FunctionPointerMethodSymbol CreateMethodFromSource(FunctionPointerTypeSymbol parent, FunctionPointerTypeSyntax syntax, Binder typeBinder, DiagnosticBag diagnostics, ConsList<TypeSymbol> basesBeingResolved)
            {
                var callingConvention = GetCallingConvention(syntax.CallingConvention.Text);
                if (callingConvention == CallingConvention.Invalid)
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
                    if (modifiers.Count > 0)
                    {
                        for (int i = 0; i < returnTypeParameter.Modifiers.Count; i++)
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
                    }

                    returnType = typeBinder.BindType(returnTypeParameter.Type, diagnostics, basesBeingResolved);

                    if (returnType.IsVoidType() && refKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_NoVoidHere, returnTypeParameter.GetLocation());
                    }
                }

                var signature = new FunctionPointerMethodSymbol(parent, callingConvention, refKind, returnType);

                if (syntax.Parameters.Count > 1)
                {
                    signature._lazyParameterSymbols = ParameterHelpers.MakeParameters(typeBinder,
                        signature,
                        syntax.Parameters,
                        arglistToken: out _,
                        diagnostics,
                        allowRefOrOut: true,
                        allowThis: false,
                        // PROTOTYPE(func-ptr): Custom modifiers for in
                        addRefReadOnlyModifier: false,
                        lastIndex: syntax.Parameters.Count - 2,
                        parsingFunctionPointer: true);
                }

                return signature;
            }

            private FunctionPointerMethodSymbol(FunctionPointerTypeSymbol parent, CallingConvention callingConvention, RefKind refKind, TypeWithAnnotations returnType)
            {
                _parent = parent;
                CallingConvention = callingConvention;
                RefKind = refKind;
                ReturnTypeWithAnnotations = returnType;
            }

            internal override CallingConvention CallingConvention { get; }
            public override bool ReturnsVoid => ReturnTypeWithAnnotations.IsVoidType();
            public override RefKind RefKind { get; }
            public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }
            public override ImmutableArray<ParameterSymbol> Parameters => _lazyParameterSymbols;
            // PROTOTYPE(func-ptr): Custom modifiers for ref readonly
            public override ImmutableArray<CustomModifier> RefCustomModifiers => throw new NotImplementedException();
            public override Symbol? ContainingSymbol => _parent;
            public override MethodKind MethodKind => MethodKind.FunctionPointerSignature;

            public override bool IsVararg => false; // PROTOTYPE(func-ptr): Varargs

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
}

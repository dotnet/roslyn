// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedInteractiveInitializerMethod : SynthesizedInstanceMethodSymbol
    {
        internal const string InitializerName = "<Initialize>";

        private readonly SourceMemberContainerTypeSymbol _containingType;
        private readonly TypeSymbol _resultType;
        private ReturnTypeState _lazyReturnType;

        private sealed class ReturnTypeState
        {
            internal ReturnTypeState(bool isAsync, TypeSymbol returnType)
            {
                this.IsAsync = isAsync;
                this.ReturnType = returnType;
            }

            internal readonly bool IsAsync;
            internal readonly TypeSymbol ReturnType;
        }

        internal SynthesizedInteractiveInitializerMethod(SourceMemberContainerTypeSymbol containingType, DiagnosticBag diagnostics)
        {
            Debug.Assert(containingType.IsScriptClass);

            _containingType = containingType;

            var compilation = containingType.DeclaringCompilation;
            var submissionReturnType = compilation.SubmissionReturnType;
            _resultType = (submissionReturnType == null) ?
                null :
                compilation.GetTypeByReflectionType(submissionReturnType, diagnostics);
        }

        public override string Name
        {
            get { return InitializerName; }
        }

        internal override bool IsScriptInitializer
        {
            get { return true; }
        }

        public override int Arity
        {
            get { return this.TypeParameters.Length; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingType; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Friend; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsAsync
        {
            get { return _lazyReturnType.IsAsync; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _containingType.Locations; }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.Ordinary; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return ImmutableArray<ParameterSymbol>.Empty; }
        }

        public override bool ReturnsVoid
        {
            get { return ReturnType.SpecialType == SpecialType.System_Void; }
        }

        public override TypeSymbol ReturnType
        {
            get { return _lazyReturnType.ReturnType; }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get { return ImmutableArray<TypeSymbol>.Empty; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                Debug.Assert(!this.IsStatic);
                Debug.Assert(!this.IsGenericMethod);
                return Cci.CallingConvention.HasThis;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override bool HasSpecialName
        {
            get { return true; }
        }

        internal override MethodImplAttributes ImplementationAttributes
        {
            get { return default(MethodImplAttributes); }
        }

        internal override bool RequiresSecurityObject
        {
            get { return false; }
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return _containingType.CalculateSyntaxOffsetInSynthesizedConstructor(localPosition, localTree, isStatic: false);
        }

        internal TypeSymbol ResultType
        {
            get { return _resultType; }
        }

        internal void SetReturnType(bool isAsync, TypeSymbol returnType)
        {
            Debug.Assert(_lazyReturnType == null);
            Interlocked.CompareExchange(ref _lazyReturnType, new ReturnTypeState(isAsync, returnType), null);
        }
    }
}

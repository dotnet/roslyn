// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedInteractiveInitializerMethod : SynthesizedInstanceMethodSymbol
    {
        internal const string InitializerName = "<Initialize>";

        private readonly SourceMemberContainerTypeSymbol _containingType;
        private readonly TypeSymbol _resultType;
        private readonly TypeSymbol _returnType;

        internal SynthesizedInteractiveInitializerMethod(SourceMemberContainerTypeSymbol containingType, DiagnosticBag diagnostics)
        {
            Debug.Assert(containingType.IsScriptClass);

            _containingType = containingType;
            CalculateReturnType(containingType.DeclaringCompilation, diagnostics, out _resultType, out _returnType);
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
            get { return true; }
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

        internal override RefKind RefKind
        {
            get { return RefKind.None; }
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
            get { return _returnType.SpecialType == SpecialType.System_Void; }
        }

        public override TypeSymbol ReturnType
        {
            get { return _returnType; }
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

        private static void CalculateReturnType(
            CSharpCompilation compilation,
            DiagnosticBag diagnostics,
            out TypeSymbol resultType,
            out TypeSymbol returnType)
        {
            var submissionReturnType = compilation.SubmissionReturnType ?? typeof(object);
            var taskT = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
            var useSiteDiagnostic = taskT.GetUseSiteDiagnostic();
            if (useSiteDiagnostic != null)
            {
                diagnostics.Add(useSiteDiagnostic, NoLocation.Singleton);
            }
            resultType = compilation.GetTypeByReflectionType(submissionReturnType, diagnostics);
            returnType = taskT.Construct(resultType);
        }
    }
}

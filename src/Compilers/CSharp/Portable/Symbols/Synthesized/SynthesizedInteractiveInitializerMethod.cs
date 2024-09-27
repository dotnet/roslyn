// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedInteractiveInitializerMethod : SynthesizedInstanceMethodSymbol
    {
        internal const string InitializerName = "<Initialize>";

        private readonly SourceMemberContainerTypeSymbol _containingType;
        private readonly TypeSymbol _resultType;
        private readonly TypeSymbol _returnType;
        private ThreeState _lazyIsNullableAnalysisEnabled;

        internal SynthesizedInteractiveInitializerMethod(SourceMemberContainerTypeSymbol containingType, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(containingType.IsScriptClass);

            _containingType = containingType;
            CalculateReturnType(containingType, diagnostics, out _resultType, out _returnType);
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

        public override RefKind RefKind
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
            get { return _returnType.IsVoidType(); }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return TypeWithAnnotations.Create(_returnType); }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
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
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
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

        internal override bool IsNullableAnalysisEnabled()
        {
            if (_lazyIsNullableAnalysisEnabled == ThreeState.Unknown)
            {
                // Return true if nullable is not disabled in compilation options or if enabled
                // in any syntax tree. This could be refined to ignore top-level methods and
                // type declarations but this simple approach matches C#8 behavior.
                var compilation = DeclaringCompilation;
                bool value = (compilation.Options.NullableContextOptions != NullableContextOptions.Disable) ||
                    compilation.SyntaxTrees.Any(static tree => ((CSharpSyntaxTree)tree).IsNullableAnalysisEnabled(new TextSpan(0, tree.Length)) == true);
                _lazyIsNullableAnalysisEnabled = value.ToThreeState();
            }
            return _lazyIsNullableAnalysisEnabled == ThreeState.True;
        }

        private static void CalculateReturnType(
            SourceMemberContainerTypeSymbol containingType,
            BindingDiagnosticBag diagnostics,
            out TypeSymbol resultType,
            out TypeSymbol returnType)
        {
            CSharpCompilation compilation = containingType.DeclaringCompilation;
            var submissionReturnTypeOpt = compilation.ScriptCompilationInfo?.ReturnTypeOpt;
            var taskT = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
            diagnostics.ReportUseSite(taskT, NoLocation.Singleton);

            // If no explicit return type is set on ScriptCompilationInfo, default to
            // System.Object from the target corlib. This allows cross compiling scripts
            // to run on a target corlib that may differ from the host compiler's corlib.
            // cf. https://github.com/dotnet/roslyn/issues/8506
            resultType = (object)submissionReturnTypeOpt == null
                ? compilation.GetSpecialType(SpecialType.System_Object)
                : compilation.GetTypeByReflectionType(submissionReturnTypeOpt, diagnostics);
            returnType = taskT.Construct(resultType);
        }

        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();
    }
}

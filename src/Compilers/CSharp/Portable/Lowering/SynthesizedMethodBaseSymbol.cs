// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A base method symbol used as a base class for lambda method symbol and base method wrapper symbol.
    /// </summary>
    internal abstract class SynthesizedMethodBaseSymbol : SourceMemberMethodSymbol
    {
        protected readonly MethodSymbol BaseMethod;
        internal TypeMap TypeMap { get; private set; }

        private readonly string _name;
        private ImmutableArray<TypeParameterSymbol> _typeParameters;
        private ImmutableArray<ParameterSymbol> _parameters;

        protected SynthesizedMethodBaseSymbol(NamedTypeSymbol containingType,
                                              MethodSymbol baseMethod,
                                              SyntaxReference syntaxReference,
                                              Location location,
                                              string name,
                                              DeclarationModifiers declarationModifiers,
                                              bool isIterator)
            : base(containingType, syntaxReference, location, isIterator,
                   (declarationModifiers, MakeFlags(
                                                    methodKind: MethodKind.Ordinary,
                                                    refKind: baseMethod.RefKind,
                                                    declarationModifiers,
                                                    returnsVoid: baseMethod.ReturnsVoid,
                                                    returnsVoidIsSet: true,
                                                    isExtensionMethod: false,
                                                    isNullableAnalysisEnabled: false,
                                                    isVarArg: baseMethod.IsVararg,
                                                    isExpressionBodied: false,
                                                    isExplicitInterfaceImplementation: false)))
        {
            Debug.Assert((object)containingType != null);
            Debug.Assert((object)baseMethod != null);

            this.BaseMethod = baseMethod;
            _name = name;
        }

        protected void AssignTypeMapAndTypeParameters(TypeMap typeMap, ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            Debug.Assert(typeMap != null);
            Debug.Assert(this.TypeMap == null);
            Debug.Assert(!typeParameters.IsDefault);
            Debug.Assert(_typeParameters.IsDefault);
            this.TypeMap = typeMap;
            _typeParameters = typeParameters;
        }

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            // TODO: move more functionality into here, making these symbols more lazy
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            // do not generate attributes for members of compiler-generated types:
            if (ContainingType.IsImplicitlyDeclared)
            {
                return;
            }

            var compilation = this.DeclaringCompilation;

            AddSynthesizedAttribute(ref attributes,
                compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public sealed override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public sealed override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            => ImmutableArray<TypeParameterConstraintKind>.Empty;

        internal override int ParameterCount
        {
            get { return this.Parameters.Length; }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_parameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _parameters, MakeParameters());
                }
                return _parameters;
            }
        }

        protected virtual ImmutableArray<TypeSymbol> ExtraSynthesizedRefParameters
        {
            get { return default(ImmutableArray<TypeSymbol>); }
        }

        protected virtual ImmutableArray<ParameterSymbol> BaseMethodParameters
        {
            get { return this.BaseMethod.Parameters; }
        }

        private ImmutableArray<ParameterSymbol> MakeParameters()
        {
            int ordinal = 0;
            var builder = ArrayBuilder<ParameterSymbol>.GetInstance();
            var parameters = this.BaseMethodParameters;
            var inheritAttributes = InheritsBaseMethodAttributes;
            foreach (var p in parameters)
            {
                builder.Add(SynthesizedParameterSymbol.Create(
                    this,
                    this.TypeMap.SubstituteType(p.OriginalDefinition.TypeWithAnnotations),
                    ordinal++,
                    p.RefKind,
                    p.Name,
                    p.EffectiveScope,
                    p.ExplicitDefaultConstantValue,
                    // the synthesized parameter doesn't need to have the same ref custom modifiers as the base
                    refCustomModifiers: default,
                    inheritAttributes ? p as SourceComplexParameterSymbolBase : null));
            }
            var extraSynthed = ExtraSynthesizedRefParameters;
            if (!extraSynthed.IsDefaultOrEmpty)
            {
                foreach (var extra in extraSynthed)
                {
                    builder.Add(SynthesizedParameterSymbol.Create(this, this.TypeMap.SubstituteType(extra), ordinal++, RefKind.Ref));
                }
            }
            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Indicates that this method inherits attributes from the base method, its parameters, return type, and type parameters.
        /// </summary>
        internal virtual bool InheritsBaseMethodAttributes => false;

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            Debug.Assert(base.GetAttributes().IsEmpty);
            return InheritsBaseMethodAttributes
                ? BaseMethod.GetAttributes()
                : ImmutableArray<CSharpAttributeData>.Empty;
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            Debug.Assert(base.GetReturnTypeAttributes().IsEmpty);
            return InheritsBaseMethodAttributes ? BaseMethod.GetReturnTypeAttributes() : ImmutableArray<CSharpAttributeData>.Empty;
        }

#nullable enable
        public sealed override DllImportData? GetDllImportData() => InheritsBaseMethodAttributes ? BaseMethod.GetDllImportData() : null;

        internal sealed override MethodImplAttributes ImplementationAttributes => InheritsBaseMethodAttributes ? BaseMethod.ImplementationAttributes : default;

        internal sealed override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => InheritsBaseMethodAttributes ? BaseMethod.ReturnValueMarshallingInformation : null;

        internal sealed override bool HasSpecialName => InheritsBaseMethodAttributes && BaseMethod.HasSpecialName;

        // Synthesized methods created from a base method with [SkipLocalsInitAttribute] will also
        // skip locals init where applicable, even if the synthesized method does not inherit attributes.
        // Note that this doesn't affect BaseMethodWrapperSymbol for example because the implementation has no locals.
        public sealed override bool AreLocalsZeroed => !(BaseMethod is SourceMethodSymbol sourceMethod) || sourceMethod.AreLocalsZeroed;

        internal sealed override bool RequiresSecurityObject => InheritsBaseMethodAttributes && BaseMethod.RequiresSecurityObject;

        internal sealed override bool HasDeclarativeSecurity => InheritsBaseMethodAttributes && BaseMethod.HasDeclarativeSecurity;

        internal sealed override IEnumerable<SecurityAttribute> GetSecurityInformation() => InheritsBaseMethodAttributes
                ? BaseMethod.GetSecurityInformation()
                : SpecializedCollections.EmptyEnumerable<SecurityAttribute>();

#nullable disable

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return this.TypeMap.SubstituteType(this.BaseMethod.OriginalDefinition.ReturnTypeWithAnnotations); }
        }

        public sealed override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => BaseMethod.ReturnTypeFlowAnalysisAnnotations;

        public sealed override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => BaseMethod.ReturnNotNullIfParameterNotNull;

        public sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public sealed override string Name
        {
            get { return _name; }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get { return true; }
        }
    }
}

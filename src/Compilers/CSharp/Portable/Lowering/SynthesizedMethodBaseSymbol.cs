// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

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
        private TypeWithAnnotations.Boxed _iteratorElementType;

        protected SynthesizedMethodBaseSymbol(NamedTypeSymbol containingType,
                                              MethodSymbol baseMethod,
                                              SyntaxReference syntaxReference,
                                              Location location,
                                              string name,
                                              DeclarationModifiers declarationModifiers)
            : base(containingType, syntaxReference, location)
        {
            Debug.Assert((object)containingType != null);
            Debug.Assert((object)baseMethod != null);

            this.BaseMethod = baseMethod;
            _name = name;

            this.MakeFlags(
                methodKind: MethodKind.Ordinary,
                declarationModifiers: declarationModifiers,
                returnsVoid: baseMethod.ReturnsVoid,
                isExtensionMethod: false,
                isMetadataVirtualIgnoringModifiers: false);
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

        protected override void MethodChecks(DiagnosticBag diagnostics)
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

        public sealed override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses()
            => ImmutableArray<TypeParameterConstraintClause>.Empty;

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
                    ImmutableInterlocked.InterlockedCompareExchange(ref _parameters, MakeParameters(), default(ImmutableArray<ParameterSymbol>));
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
                    attributes: inheritAttributes ? p.GetAttributes() : default));
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

        public sealed override RefKind RefKind
        {
            get { return this.BaseMethod.RefKind; }
        }

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return this.TypeMap.SubstituteType(this.BaseMethod.OriginalDefinition.ReturnTypeWithAnnotations); }
        }

        public sealed override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => BaseMethod.ReturnTypeFlowAnalysisAnnotations;

        public sealed override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => BaseMethod.ReturnNotNullIfParameterNotNull;

        public sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public sealed override bool IsVararg
        {
            get { return this.BaseMethod.IsVararg; }
        }

        public sealed override string Name
        {
            get { return _name; }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool IsExpressionBodied
        {
            get { return false; }
        }

        internal override TypeWithAnnotations IteratorElementTypeWithAnnotations
        {
            get
            {
                if (_iteratorElementType is null)
                {
                    Interlocked.CompareExchange(ref _iteratorElementType,
                                                new TypeWithAnnotations.Boxed(TypeMap.SubstituteType(BaseMethod.IteratorElementTypeWithAnnotations.Type)),
                                                null);
                }

                return _iteratorElementType.Value;
            }
            set
            {
                Debug.Assert(!value.IsDefault);
                Interlocked.Exchange(ref _iteratorElementType, new TypeWithAnnotations.Boxed(value));
            }
        }
    }
}

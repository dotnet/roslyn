// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A base method symbol used as a base class for lambda method symbol and base method wrapper symbol.
    /// </summary>
    internal abstract class SynthesizedMethodBaseSymbol : SourceMethodSymbol
    {
        protected readonly MethodSymbol BaseMethod;
        internal TypeMap TypeMap { get; private set; }

        private readonly string _name;
        private ImmutableArray<TypeParameterSymbol> _typeParameters;
        private ImmutableArray<ParameterSymbol> _parameters;
        private TypeSymbol _iteratorElementType;

        protected SynthesizedMethodBaseSymbol(NamedTypeSymbol containingType,
                                              MethodSymbol baseMethod,
                                              SyntaxReference syntaxReference,
                                              SyntaxReference blockSyntaxReference,
                                              Location location,
                                              string name,
                                              DeclarationModifiers declarationModifiers)
            : base(containingType, syntaxReference, blockSyntaxReference, location)
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

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

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
            foreach (var p in parameters)
            {
                builder.Add(new SynthesizedParameterSymbol(this, this.TypeMap.SubstituteType(p.OriginalDefinition.Type).Type, ordinal++, p.RefKind, p.Name));
            }
            var extraSynthed = ExtraSynthesizedRefParameters;
            if (!extraSynthed.IsDefaultOrEmpty)
            {
                foreach (var extra in extraSynthed)
                {
                    builder.Add(new SynthesizedParameterSymbol(this, this.TypeMap.SubstituteType(extra).Type, ordinal++, RefKind.Ref));
                }
            }
            return builder.ToImmutableAndFree();
        }

        public sealed override TypeSymbol ReturnType
        {
            get { return this.TypeMap.SubstituteType(this.BaseMethod.OriginalDefinition.ReturnType).Type; }
        }

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

        internal override TypeSymbol IteratorElementType
        {
            get
            {
                if (_iteratorElementType == null)
                {
                    _iteratorElementType = TypeMap.SubstituteType(BaseMethod.IteratorElementType).Type;
                }
                return _iteratorElementType;
            }
            set
            {
                Debug.Assert(value != null);
                _iteratorElementType = value;
            }
        }
    }
}

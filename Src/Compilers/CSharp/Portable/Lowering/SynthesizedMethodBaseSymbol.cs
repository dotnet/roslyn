// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        protected TypeMap TypeMap { get; private set; }

        private readonly string name;
        private ImmutableArray<TypeParameterSymbol> typeParameters;
        private ImmutableArray<ParameterSymbol> parameters;

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
            this.name = name;

            this.flags = MakeFlags(
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
            Debug.Assert(this.typeParameters.IsDefault);
            this.TypeMap = typeMap;
            this.typeParameters = typeParameters;
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
                compilation.SynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return typeParameters; }
        }

        internal override int ParameterCount
        {
            get { return this.BaseMethod.ParameterCount; }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (parameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref parameters, MakeParameters(), default(ImmutableArray<ParameterSymbol>));
                }
                return parameters;
            }
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
                builder.Add(new SynthesizedParameterSymbol(this, this.TypeMap.SubstituteType(((ParameterSymbol)p.OriginalDefinition).Type), ordinal++, p.RefKind, p.Name));
            }
            return builder.ToImmutableAndFree();
        }

        public sealed override TypeSymbol ReturnType
        {
            get { return this.TypeMap.SubstituteType(((MethodSymbol)this.BaseMethod.OriginalDefinition).ReturnType); }
        }

        public sealed override bool IsVararg
        {
            get { return this.BaseMethod.IsVararg; }
        }

        public sealed override string Name
        {
            get { return name; }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool IsExpressionBodied
        {
            get { return false; }
        }
    }
}

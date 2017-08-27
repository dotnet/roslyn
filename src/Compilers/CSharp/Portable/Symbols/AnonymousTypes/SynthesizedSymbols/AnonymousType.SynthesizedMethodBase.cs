﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents a base implementation for anonymous type synthesized methods.
        /// </summary>
        private abstract class SynthesizedMethodBase : SynthesizedInstanceMethodSymbol
        {
            private readonly NamedTypeSymbol _containingType;
            private readonly string _name;

            public SynthesizedMethodBase(NamedTypeSymbol containingType, string name)
            {
                _containingType = containingType;
                _name = name;
            }

            internal sealed override bool GenerateDebugInfo
            {
                get { return false; }
            }

            public sealed override int Arity
            {
                get { return 0; }
            }

            public sealed override Symbol ContainingSymbol
            {
                get { return _containingType; }
            }

            public override NamedTypeSymbol ContainingType
            {
                get
                {
                    return _containingType;
                }
            }

            public override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray<Location>.Empty; }
            }

            public sealed override Accessibility DeclaredAccessibility
            {
                get { return Accessibility.Public; }
            }

            public sealed override bool IsStatic
            {
                get { return false; }
            }

            public sealed override bool IsVirtual
            {
                get { return false; }
            }

            public sealed override bool IsAsync
            {
                get { return false; }
            }

            internal sealed override System.Reflection.MethodImplAttributes ImplementationAttributes
            {
                get { return default(System.Reflection.MethodImplAttributes); }
            }

            internal sealed override Cci.CallingConvention CallingConvention
            {
                get { return Cci.CallingConvention.HasThis; }
            }

            public sealed override bool IsExtensionMethod
            {
                get { return false; }
            }

            public sealed override bool HidesBaseMethodsByName
            {
                get { return false; }
            }

            public sealed override bool IsVararg
            {
                get { return false; }
            }

            public sealed override ImmutableArray<TypeSymbol> TypeArguments
            {
                get { return ImmutableArray<TypeSymbol>.Empty; }
            }

            public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get { return ImmutableArray<TypeParameterSymbol>.Empty; }
            }

            internal sealed override bool IsExplicitInterfaceImplementation
            {
                get { return false; }
            }

            public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
            {
                get { return ImmutableArray<MethodSymbol>.Empty; }
            }

            public sealed override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
            {
                get { return ImmutableArray<CustomModifier>.Empty; }
            }

            public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
            {
                get { return ImmutableArray<CustomModifier>.Empty; }
            }

            public override Symbol AssociatedSymbol
            {
                get { return null; }
            }

            public sealed override bool IsAbstract
            {
                get { return false; }
            }

            public sealed override bool IsSealed
            {
                get { return false; }
            }

            public sealed override bool IsExtern
            {
                get { return false; }
            }

            public sealed override string Name
            {
                get { return _name; }
            }

            internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
            {
                return false;
            }

            internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(compilationState, ref attributes);

                AddSynthesizedAttribute(ref attributes, Manager.Compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));
            }

            protected AnonymousTypeManager Manager
            {
                get
                {
                    AnonymousTypeTemplateSymbol template = _containingType as AnonymousTypeTemplateSymbol;
                    return ((object)template != null) ? template.Manager : ((AnonymousTypePublicSymbol)_containingType).Manager;
                }
            }

            internal sealed override bool RequiresSecurityObject
            {
                get { return false; }
            }

            public sealed override DllImportData GetDllImportData()
            {
                return null;
            }

            internal sealed override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
            {
                get { return null; }
            }

            internal sealed override bool HasDeclarativeSecurity
            {
                get { return false; }
            }

            internal sealed override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
            {
                return ImmutableArray<string>.Empty;
            }

            internal override bool SynthesizesLoweredBoundBody
            {
                get
                {
                    return true;
                }
            }

            protected SyntheticBoundNodeFactory CreateBoundNodeFactory(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
                F.CurrentMethod = this;
                return F;
            }

            internal sealed override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}

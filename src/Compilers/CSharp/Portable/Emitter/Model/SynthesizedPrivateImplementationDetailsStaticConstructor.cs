// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedPrivateImplementationDetailsStaticConstructor : SynthesizedGlobalMethodSymbol
    {
        internal SynthesizedPrivateImplementationDetailsStaticConstructor(SourceModuleSymbol containingModule, PrivateImplementationDetails privateImplementationType, NamedTypeSymbol voidType)
            : base(containingModule, privateImplementationType, voidType, WellKnownMemberNames.StaticConstructorName)
        {
            this.SetParameters(ImmutableArray<ParameterSymbol>.Empty);
        }

        public override MethodKind MethodKind => MethodKind.StaticConstructor;

        internal override bool HasSpecialName => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            factory.CurrentMethod = this;
            
            // MVID = TypeOf(PrivateImplementationDetails).Module.ModuleVersionId;

            BoundStatement mvidInitialization =
                factory.Assignment(
                    factory.ModuleVersionId(),
                    factory.Property(
                        factory.Property(
                            factory.TypeOfPrivateImplementationDetails(),
                            WellKnownMember.System_Type__Module),
                        WellKnownMember.System_Reflection_Module__ModuleVersionId));

            BoundStatement returnStatement = factory.Return();

            factory.CloseMethod(factory.Block(ImmutableArray.Create(mvidInitialization, returnStatement)));
        }
    }
}

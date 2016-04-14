// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedPrivateImplementationDetailsStaticConstructor : SynthesizedGlobalMethodSymbol
    {
        private readonly PrivateImplementationDetails _details;

        internal SynthesizedPrivateImplementationDetailsStaticConstructor(SourceModuleSymbol containingModule, PrivateImplementationDetails privateImplementationType, NamedTypeSymbol voidType)
            : base(containingModule, privateImplementationType, voidType, WellKnownMemberNames.StaticConstructorName)
        {
            this.SetParameters(ImmutableArray<ParameterSymbol>.Empty);
            _details = privateImplementationType;
        }

        public override MethodKind MethodKind => MethodKind.StaticConstructor;

        internal override bool HasSpecialName => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            factory.CurrentMethod = this;

            ArrayBuilder<BoundStatement> body = ArrayBuilder<BoundStatement>.GetInstance();

            // Initialize the payload arrays for each kind of dynamic analysis instrumentation.

            ImmutableArray<InstrumentationPayloadField> payloadFields = _details.GetInstrumentationPayloads();
            for (int index = 0; index < payloadFields.Length; index++)
            {
                InstrumentationPayloadField payloadField = payloadFields[index];
                if (payloadField != null)
                {
                    ArrayTypeSymbol payloadArrayType = (ArrayTypeSymbol)payloadField.Type;

                    BoundStatement payloadInitialization =
                        factory.Assignment(
                            factory.InstrumentationPayload(index, payloadArrayType),
                            factory.Array(payloadArrayType.ElementType, factory.Binary(BinaryOperatorKind.Addition, factory.SpecialType(SpecialType.System_Int32), factory.GreatestMethodDefinitionToken(), factory.Literal(1))));
                    body.Add(payloadInitialization);
                }
            }

            // MVID = TypeOf(PrivateImplementationDetails).Module.ModuleVersionId;

            BoundStatement mvidInitialization =
                factory.Assignment(
                    factory.ModuleVersionId(),
                    factory.Property(
                        factory.Property(
                            factory.TypeOfPrivateImplementationDetails(),
                            WellKnownMember.System_Type__Module),
                        WellKnownMember.System_Reflection_Module__ModuleVersionId));
            body.Add(mvidInitialization);

            BoundStatement returnStatement = factory.Return();
            body.Add(returnStatement);

            factory.CloseMethod(factory.Block(body.ToImmutableAndFree()));
        }
    }
}

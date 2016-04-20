// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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

            // Initialize the payload root for for each kind of dynamic analysis instrumentation.
            // A payload root is an array of arrays of per-method instrumentation payloads.
            // For each kind of instrumentation:
            //
            //     payloadRoot = new T[MaximumMethodDefIndex + 1][];
            //
            // where T is the type of the payload at each instrumentation point, and MaximumMethodDefIndex is the 
            // index portion of the greatest method definition token in the compilation. This guarantees that any
            // method can use the index portion of its own method definition token as an index into the payload array.

            IReadOnlyCollection<KeyValuePair<int, InstrumentationPayloadRootField>> payloadRootFields = ContainingPrivateImplementationDetailsType.GetInstrumentationPayloadRoots();
            Debug.Assert(payloadRootFields.Count > 0);

            ArrayBuilder<BoundStatement> body = ArrayBuilder<BoundStatement>.GetInstance(2 + payloadRootFields.Count);
            foreach (KeyValuePair<int, InstrumentationPayloadRootField> payloadRoot in payloadRootFields.OrderBy(analysis => analysis.Key))
            {
                int analysisKind = payloadRoot.Key;
                ArrayTypeSymbol payloadArrayType = (ArrayTypeSymbol)payloadRoot.Value.Type;

                BoundStatement payloadInitialization =
                    factory.Assignment(
                        factory.InstrumentationPayloadRoot(analysisKind, payloadArrayType),
                        factory.Array(payloadArrayType.ElementType, factory.Binary(BinaryOperatorKind.Addition, factory.SpecialType(SpecialType.System_Int32), factory.MaximumMethodDefIndex(), factory.Literal(1))));
                body.Add(payloadInitialization);
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

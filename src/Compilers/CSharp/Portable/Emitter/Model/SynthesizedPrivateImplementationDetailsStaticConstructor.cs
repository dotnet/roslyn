// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedPrivateImplementationDetailsStaticConstructor : SynthesizedGlobalMethodSymbol
    {
        internal SynthesizedPrivateImplementationDetailsStaticConstructor(SynthesizedPrivateImplementationDetailsType privateImplementationType, NamedTypeSymbol voidType)
            : base(privateImplementationType, voidType, WellKnownMemberNames.StaticConstructorName)
        {
            this.SetParameters(ImmutableArray<ParameterSymbol>.Empty);
        }

        public override MethodKind MethodKind => MethodKind.StaticConstructor;

        internal override bool HasSpecialName => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            CSharpSyntaxNode syntax = this.GetNonNullSyntaxNode();
            SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(this, syntax, compilationState, diagnostics);
            factory.CurrentFunction = this;
            ArrayBuilder<BoundStatement> body = ArrayBuilder<BoundStatement>.GetInstance();

            // Initialize the payload root for each kind of dynamic analysis instrumentation.
            // A payload root is an array of arrays of per-method instrumentation payloads.
            // For each kind of instrumentation:
            //
            //     payloadRoot = new T[MaximumMethodDefIndex + 1][];
            //
            // where T is the type of the payload at each instrumentation point, and MaximumMethodDefIndex is the
            // index portion of the greatest method definition token in the compilation. This guarantees that any
            // method can use the index portion of its own method definition token as an index into the payload array.

            try
            {
                foreach (KeyValuePair<int, InstrumentationPayloadRootField> payloadRoot in ContainingPrivateImplementationDetailsType.GetInstrumentationPayloadRoots())
                {
                    int analysisKind = payloadRoot.Key;
                    ArrayTypeSymbol payloadArrayType = (ArrayTypeSymbol)payloadRoot.Value.Type.GetInternalSymbol();

                    BoundStatement payloadInitialization =
                        factory.Assignment(
                            factory.InstrumentationPayloadRoot(analysisKind, payloadArrayType),
                            factory.Array(payloadArrayType.ElementType, factory.Binary(BinaryOperatorKind.Addition, factory.SpecialType(SpecialType.System_Int32), factory.MaximumMethodDefIndex(), factory.Literal(1))));
                    body.Add(payloadInitialization);
                }

                // Initialize the module version ID (MVID) field. Dynamic instrumentation requires the MVID of the executing module, and this field makes that accessible.
                // MVID = new Guid(ModuleVersionIdString);
                body.Add(
                    factory.Assignment(
                       factory.ModuleVersionId(),
                       factory.New(
                           factory.WellKnownMethod(WellKnownMember.System_Guid__ctor),
                           factory.ModuleVersionIdString())));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember missing)
            {
                diagnostics.Add(missing.Diagnostic);
            }

            BoundStatement returnStatement = factory.Return();
            body.Add(returnStatement);

            factory.CloseMethod(factory.Block(body.ToImmutableAndFree()));
        }
    }
}

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
            CSharpSyntaxNode syntax = this.GetNonNullSyntaxNode();
            SyntheticBoundNodeFactory factory = new SyntheticBoundNodeFactory(this, syntax, compilationState, diagnostics);
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

            // Initialize the module version ID (MVID) field. Dynamic instrumentation requires the MVID of the executing module, and this field makes that accessible.
            body.Add(InitializeMVID(factory, diagnostics));

            BoundStatement returnStatement = factory.Return();
            body.Add(returnStatement);

            factory.CloseMethod(factory.Block(body.ToImmutableAndFree()));
        }

        private BoundStatement InitializeMVID(SyntheticBoundNodeFactory factory, DiagnosticBag diagnostics)
        {

            CSharpCompilation compilation = factory.Compilation;
            CSharpSyntaxNode syntax = factory.Syntax;

            // It is necessary to reflect over a type defined in the module. The PrivateImplementationDetails class serves as well as any other type
            // for this purpose, and happens to be available here.
            BoundExpression typeInstance = factory.TypeOfPrivateImplementationDetails();

            // Getting to the MVID requires getting to a representation of the current module. If the target platform is Desktop, the module is
            // available as a property of a type instance. If the target platform is Portable, a type instance does not have a module property.
            // Instead, the module is available as a property of a member info object, which is available as a base type of a type info object,
            // which is available via a GetTypeInfo extension method.
            //
            // The Portable mechanism also works on Desktop, but not on versions prior to 4.5, so there is no one single mechanism that work on all platforms.

            BoundExpression moduleReference;
            Symbol system_Type__Module = compilation.GetWellKnownTypeMember(WellKnownMember.System_Type__Module);
            if (system_Type__Module != null)
            {
                // moduleReference = TypeOf(PrivateImplementationDetails).Module;
                moduleReference = factory.Property(typeInstance, WellKnownMember.System_Type__Module);
            }

            else
            {
                // moduleReference = ((System.Reflection.MemberInfo)System.Reflection.IntrospectionExtensions.GetTypeInfo(TypeOf(PrivateImplementationDetails))).Module;
                moduleReference =
                    factory.Property(
                        factory.Convert(
                            factory.WellKnownType(WellKnownType.System_Reflection_MemberInfo),
                            factory.StaticCall(
                                factory.WellKnownType(WellKnownType.System_Reflection_IntrospectionExtensions),
                                WellKnownMember.System_Reflection_IntrospectionExtensions__GetTypeInfo,
                                typeInstance),
                            ConversionKind.ImplicitReference),
                        WellKnownMember.System_Reflection_MemberInfo__Module);
            }

            // MVID = moduleReference.ModuleVersionId;
             return
                factory.Assignment(
                    factory.ModuleVersionId(),
                    factory.Property(
                        moduleReference,
                        WellKnownMember.System_Reflection_Module__ModuleVersionId));
        }
    }
}

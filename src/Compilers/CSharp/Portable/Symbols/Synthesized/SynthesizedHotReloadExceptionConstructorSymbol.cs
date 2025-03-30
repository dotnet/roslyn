﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedHotReloadExceptionConstructorSymbol : SynthesizedInstanceConstructor
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal SynthesizedHotReloadExceptionConstructorSymbol(NamedTypeSymbol containingType, TypeSymbol stringType, TypeSymbol intType) :
            base(containingType)
        {
            _parameters =
            [
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(stringType), ordinal: 0, RefKind.None),
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(intType), ordinal: 1, RefKind.None)
            ];
        }

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        /// <summary>
        /// Exception message.
        /// </summary>
        public ParameterSymbol MessageParameter => _parameters[0];

        /// <summary>
        /// Integer value of <see cref="HotReloadExceptionCode"/>.
        /// </summary>
        public ParameterSymbol CodeParameter => _parameters[1];

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var containingType = (SynthesizedHotReloadExceptionSymbol)ContainingType;

            var factory = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            factory.CurrentFunction = this;

            var exceptionConstructor = (MethodSymbol?)factory.WellKnownMember(WellKnownMember.System_Exception__ctorString, isOptional: true);
            if (exceptionConstructor is null)
            {
                diagnostics.Add(ErrorCode.ERR_EncUpdateFailedMissingSymbol,
                    Location.None,
                    CodeAnalysisResources.Constructor,
                    "System.Exception..ctor(string)");

                factory.CloseMethod(factory.Block());
                return;
            }

            var block = factory.Block(
                ImmutableArray.Create<BoundStatement>(
                    factory.ExpressionStatement(factory.Call(
                        factory.This(),
                        exceptionConstructor,
                        factory.Parameter(MessageParameter))),
                    factory.Assignment(factory.Field(factory.This(), containingType.CodeField), factory.Parameter(CodeParameter)),
                    factory.Return()
                ));

            factory.CloseMethod(block);
        }
    }
}

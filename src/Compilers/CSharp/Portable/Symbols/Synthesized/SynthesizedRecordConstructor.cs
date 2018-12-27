// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordConstructor :  SynthesizedInstanceConstructor
    {
        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public SynthesizedRecordConstructor(
            NamedTypeSymbol containingType,
            Binder parameterBinder,
            ParameterListSyntax parameterList,
            DiagnosticBag diagnostics)
            : base(containingType)
        {
            Parameters = ParameterHelpers.MakeParameters(
                parameterBinder,
                this,
                parameterList,
                out _,
                diagnostics,
                allowRefOrOut: true,
                allowThis: false,
                addRefReadOnlyModifier: false);
        }
    }
}

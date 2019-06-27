// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordConstructor : SynthesizedInstanceConstructor
    {
        public override ImmutableArray<ParameterSymbol> Parameters { get; }
        public ImmutableArray<SynthesizedRecordPropertySymbol> Properties { get; }

        public SynthesizedRecordConstructor(
            NamedTypeSymbol containingType,
            Binder binder,
            BaseParameterListSyntax paramList,
            DiagnosticBag diagnostics)
            : base(containingType)
        {
            Parameters = ParameterHelpers.MakeParameters(
                binder,
                this,
                paramList,
                out _,
                diagnostics,
                allowRefOrOut: true,
                allowThis: false,
                addRefReadOnlyModifier: false);

            var propertiesBuilder = ArrayBuilder<SynthesizedRecordPropertySymbol>.GetInstance(Parameters.Length);
            foreach (ParameterSymbol param in Parameters)
            {
                propertiesBuilder.Add(new SynthesizedRecordPropertySymbol(containingType, param));
            }
            Properties = propertiesBuilder.ToImmutableAndFree();
        }

        internal BoundBlock MakeMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            //  Method body:
            //
            //  {
            //      Object..ctor();
            //      this.backingField_1 = arg1;
            //      ...
            //      this.backingField_N = argN;
            //  }
            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            int paramCount = this.ParameterCount;

            // List of statements
            var statements = ArrayBuilder<BoundStatement>.GetInstance(paramCount + 1);

            // Assign fields
            for (int index = 0; index < paramCount; index++)
            {
                // Generate 'field' = 'parameter' statement
                statements.Add(
                    F.Assignment(F.Field(F.This(), Properties[index].BackingField), F.Parameter(Parameters[index])));
            }

            // Final return statement
            statements.Add(F.Return());

            // Create a bound block 
            return F.Block(statements.ToImmutableAndFree());
        }
    }
}

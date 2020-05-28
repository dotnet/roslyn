// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordCopyCtor : SynthesizedInstanceConstructor
    {
        public SynthesizedRecordCopyCtor(
            SourceMemberContainerTypeSymbol containingType,
            DiagnosticBag diagnostics)
            : base(containingType)
        {
            Debug.Assert(!containingType.IsStructType(), "only reference types should define copy constructors");
            Parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(
                this,
                TypeWithAnnotations.Create(
                    isNullableEnabled: true,
                    ContainingType),
                ordinal: 0,
                RefKind.None));
        }

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            // We need a separate sort key because struct records will have two synthesized
            // constructors: the record constructor, and the parameterless constructor
            return LexicalSortKey.SynthesizedRecordCopyCtor;
        }

        internal override void GenerateMethodBodyStatements(SyntheticBoundNodeFactory F, ArrayBuilder<BoundStatement> statements, DiagnosticBag diagnostics)
        {
            // Write assignments to backing fields
            //
            // {
            //     this.backingField1 = parameter.backingField1
            //     ...
            //     this.backingFieldN = parameter.backingFieldN
            // }
            var param = F.Parameter(Parameters[0]);
            foreach (var member in ContainingType.GetMembers())
            {
                if (member is FieldSymbol { IsStatic: false } field)
                {
                    statements.Add(F.Assignment(F.Field(F.This(), field), F.Field(param, field)));
                }
            }
        }
    }
}

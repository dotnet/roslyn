// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordCopyCtor : SynthesizedInstanceConstructor
    {
        private readonly int _memberOffset;

        public SynthesizedRecordCopyCtor(
            SourceMemberContainerTypeSymbol containingType,
            int memberOffset)
            : base(containingType)
        {
            _memberOffset = memberOffset;
            Parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(
                this,
                TypeWithAnnotations.Create(
                    isNullableEnabled: true,
                    ContainingType),
                ordinal: 0,
                RefKind.None));
        }

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public override Accessibility DeclaredAccessibility => Accessibility.Protected;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        internal override void GenerateMethodBodyStatements(SyntheticBoundNodeFactory F, ArrayBuilder<BoundStatement> statements, DiagnosticBag diagnostics)
        {
            // Tracking issue for copy constructor in inheritance scenario: https://github.com/dotnet/roslyn/issues/44902
            // Write assignments to fields
            // .ctor(DerivedRecordType original) : base((BaseRecordType)original)
            // {
            //     this.field1 = parameter.field1
            //     ...
            //     this.fieldN = parameter.fieldN
            // }
            var param = F.Parameter(Parameters[0]);
            foreach (var field in ContainingType.GetFieldsToEmit())
            {
                if (!field.IsStatic)
                {
                    statements.Add(F.Assignment(F.Field(F.This(), field), F.Field(param, field)));
                }
            }
        }

        internal static MethodSymbol? FindCopyConstructor(NamedTypeSymbol containingType, NamedTypeSymbol within, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // We should handle ambiguities once we consider custom modifiers, as we do in overload resolution
            // https://github.com/dotnet/roslyn/issues/45077
            foreach (var member in containingType.InstanceConstructors)
            {
                if (HasCopyConstructorSignature(member) &&
                    AccessCheck.IsSymbolAccessible(member.OriginalDefinition, within.OriginalDefinition, ref useSiteDiagnostics))
                {
                    return member;
                }
            }

            return null;
        }

        internal static bool IsCopyConstructor(Symbol member)
        {
            if (member is MethodSymbol { MethodKind: MethodKind.Constructor } method)
            {
                return HasCopyConstructorSignature(method);
            }

            return false;
        }

        internal static bool HasCopyConstructorSignature(MethodSymbol member)
        {
            NamedTypeSymbol containingType = member.ContainingType;
            // We should relax the comparison to AllIgnoreOptions, so that a copy constructor with a custom modifier is recognized
            // https://github.com/dotnet/roslyn/issues/45077
            return member is MethodSymbol { IsStatic: false, ParameterCount: 1, Arity: 0 } method &&
                method.Parameters[0].Type.Equals(containingType, TypeCompareKind.CLRSignatureCompareOptions) &&
                method.Parameters[0].RefKind == RefKind.None;
        }
    }
}

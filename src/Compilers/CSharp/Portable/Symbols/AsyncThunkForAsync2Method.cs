// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class AsyncThunkForAsync2Method : WrappedMethodSymbol
    {
        readonly TypeWithAnnotations _taskType;

        internal AsyncThunkForAsync2Method(MethodSymbol underlyingMethod, TypeWithAnnotations taskType)
        {
            Debug.Assert(underlyingMethod.IsAsync2);
            UnderlyingMethod = underlyingMethod;
            _taskType = taskType;
        }

        public override MethodSymbol UnderlyingMethod { get; }

        public override Symbol? AssociatedSymbol => UnderlyingMethod;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => _taskType;

        public override bool ReturnsVoid => false;

        internal override bool IsAsync2 => false;

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => UnderlyingMethod.TypeArgumentsWithAnnotations;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => UnderlyingMethod.TypeParameters;

        public override ImmutableArray<ParameterSymbol> Parameters => UnderlyingMethod.Parameters;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => UnderlyingMethod.ExplicitInterfaceImplementations;

        public override Symbol ContainingSymbol => UnderlyingMethod.ContainingSymbol;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => UnderlyingMethod.CalculateLocalSyntaxOffset(localPosition, localTree);

        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => UnderlyingMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);

        internal override bool IsNullableAnalysisEnabled() => UnderlyingMethod.IsNullableAnalysisEnabled();
    }
}

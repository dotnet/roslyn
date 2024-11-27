// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

internal class RuntimeAsyncMethodSymbol : WrappedMethodSymbol
{
    public RuntimeAsyncMethodSymbol(MethodSymbol underlyingMethod, CSharpCompilation compilation)
    {
#if DEBUG
        var returnTypeOriginalDefinition = underlyingMethod.ReturnType.OriginalDefinition;
        Debug.Assert(
            returnTypeOriginalDefinition.Equals(compilation.CommonGetWellKnownType(WellKnownType.System_Threading_Tasks_Task))
            || returnTypeOriginalDefinition.Equals(compilation.CommonGetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T))
            || returnTypeOriginalDefinition.Equals(compilation.CommonGetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask))
            || returnTypeOriginalDefinition.Equals(compilation.CommonGetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask_T)));
        Debug.Assert(underlyingMethod.RefKind == RefKind.None && underlyingMethod.RefCustomModifiers.IsEmpty);
#endif

        UnderlyingMethod = underlyingMethod;

        var originalReturnType = (NamedTypeSymbol)UnderlyingMethod.ReturnType;
        var returnType = originalReturnType.Arity == 0
            ? TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Void))
            : originalReturnType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
        ReturnTypeWithAnnotations = returnType.WithModifiers([CSharpCustomModifier.CreateRequired(originalReturnType.OriginalDefinition), .. returnType.CustomModifiers]);
    }

    public override MethodSymbol UnderlyingMethod { get; }
    public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }

    public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => throw ExceptionUtilities.Unreachable();

    #region Forwards
    public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => UnderlyingMethod.TypeArgumentsWithAnnotations;

    public override ImmutableArray<TypeParameterSymbol> TypeParameters => UnderlyingMethod.TypeParameters;

    public override ImmutableArray<ParameterSymbol> Parameters => UnderlyingMethod.Parameters;

    public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingMethod.RefCustomModifiers;

    public override Symbol AssociatedSymbol => UnderlyingMethod.AssociatedSymbol;

    public override Symbol ContainingSymbol => UnderlyingMethod.ContainingSymbol;

    internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => UnderlyingMethod.CalculateLocalSyntaxOffset(localPosition, localTree);

    internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => UnderlyingMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);

    internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument) => UnderlyingMethod.HasAsyncMethodBuilderAttribute(out builderArgument);

    internal override bool IsNullableAnalysisEnabled() => UnderlyingMethod.IsNullableAnalysisEnabled();
    #endregion
}

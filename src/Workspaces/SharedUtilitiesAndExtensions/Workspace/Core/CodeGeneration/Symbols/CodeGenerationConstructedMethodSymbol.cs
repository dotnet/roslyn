// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal sealed class CodeGenerationConstructedMethodSymbol : CodeGenerationAbstractMethodSymbol
{
    private readonly CodeGenerationAbstractMethodSymbol _constructedFrom;
    private readonly ImmutableArray<ITypeSymbol> _typeArguments;

    public CodeGenerationConstructedMethodSymbol(
        CodeGenerationAbstractMethodSymbol constructedFrom,
        ImmutableArray<ITypeSymbol> typeArguments)
        : base(constructedFrom.ContainingType,
               constructedFrom.GetAttributes(),
               constructedFrom.DeclaredAccessibility,
               constructedFrom.Modifiers,
               constructedFrom.Name,
               constructedFrom.GetReturnTypeAttributes())
    {
        _constructedFrom = constructedFrom;
        this.OriginalDefinition = _constructedFrom.OriginalDefinition;
        _typeArguments = typeArguments;
    }

    public override int Arity => _constructedFrom.Arity;

    public override bool ReturnsVoid => _constructedFrom.ReturnsVoid;

    public override bool ReturnsByRef => _constructedFrom.ReturnsByRef;

    public override RefKind RefKind => _constructedFrom.RefKind;

    public override bool ReturnsByRefReadonly
    {
        get
        {
            return _constructedFrom.ReturnsByRefReadonly;
        }
    }

    public override ITypeSymbol ReturnType
    {
        get
        {
            // TODO(cyrusn): Construct this.
            return _constructedFrom.ReturnType;
        }
    }

    public override ImmutableArray<ITypeSymbol> TypeArguments => _typeArguments;

    public override ImmutableArray<ITypeParameterSymbol> TypeParameters => _constructedFrom.TypeParameters;

    public override ImmutableArray<IParameterSymbol> Parameters
    {
        get
        {
            // TODO(cyrusn): Construct this.
            return this.OriginalDefinition.Parameters;
        }
    }

    public override IMethodSymbol ConstructedFrom => _constructedFrom;

    public override bool IsReadOnly => _constructedFrom.IsReadOnly;
    public override bool IsInitOnly => _constructedFrom.IsInitOnly;

    public override System.Reflection.MethodImplAttributes MethodImplementationFlags => _constructedFrom.MethodImplementationFlags;

    public override IMethodSymbol OverriddenMethod
            // TODO(cyrusn): Construct this.
            => _constructedFrom.OverriddenMethod;

    public override IMethodSymbol ReducedFrom
            // TODO(cyrusn): Construct this.
            => _constructedFrom.ReducedFrom;

    public override ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
        => throw new System.InvalidOperationException();

    public override IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
    {
        // TODO(cyrusn): support this properly.
        return null;
    }

    public override ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations
            // TODO(cyrusn): Construct this.
            => _constructedFrom.ExplicitInterfaceImplementations;

    public override IMethodSymbol PartialDefinitionPart
            // TODO(cyrusn): Construct this.
            => _constructedFrom.PartialDefinitionPart;

    public override IMethodSymbol PartialImplementationPart
            // TODO(cyrusn): Construct this.
            => _constructedFrom.PartialImplementationPart;

    public override bool IsPartialDefinition => _constructedFrom.IsPartialDefinition;

    protected override CodeGenerationSymbol Clone()
        => new CodeGenerationConstructedMethodSymbol(_constructedFrom, _typeArguments);
}

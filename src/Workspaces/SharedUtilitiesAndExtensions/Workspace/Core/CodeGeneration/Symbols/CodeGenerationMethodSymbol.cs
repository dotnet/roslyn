// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal partial class CodeGenerationMethodSymbol : CodeGenerationAbstractMethodSymbol
{
    public override ITypeSymbol ReturnType { get; }
    public override ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }
    public override ImmutableArray<IParameterSymbol> Parameters { get; }
    public override ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations { get; }
    public override MethodKind MethodKind { get; }

    public CodeGenerationMethodSymbol(
        INamedTypeSymbol containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        ITypeSymbol returnType,
        RefKind refKind,
        ImmutableArray<IMethodSymbol> explicitInterfaceImplementations,
        string name,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<AttributeData> returnTypeAttributes,
        string documentationCommentXml = null,
        MethodKind methodKind = MethodKind.Ordinary,
        bool isInitOnly = false)
        : base(containingType, attributes, declaredAccessibility, modifiers, name, returnTypeAttributes, documentationCommentXml)
    {
        this.ReturnType = returnType;
        this.RefKind = refKind;

        Debug.Assert(!isInitOnly || methodKind == MethodKind.PropertySet);
        this.IsInitOnly = methodKind == MethodKind.PropertySet && isInitOnly;

        this.TypeParameters = typeParameters.NullToEmpty();
        this.Parameters = parameters.NullToEmpty();
        this.MethodKind = methodKind;

        this.ExplicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
        this.OriginalDefinition = this;
    }

    protected override CodeGenerationSymbol Clone()
    {
        var result = new CodeGenerationMethodSymbol(this.ContainingType,
            this.GetAttributes(), this.DeclaredAccessibility, this.Modifiers,
            this.ReturnType, this.RefKind, this.ExplicitInterfaceImplementations,
            this.Name, this.TypeParameters, this.Parameters, this.GetReturnTypeAttributes(),
            _documentationCommentXml, this.MethodKind, this.IsInitOnly);

        CodeGenerationMethodInfo.Attach(result,
            CodeGenerationMethodInfo.GetIsNew(this),
            CodeGenerationMethodInfo.GetIsUnsafe(this),
            CodeGenerationMethodInfo.GetIsPartial(this),
            CodeGenerationMethodInfo.GetIsAsyncMethod(this),
            CodeGenerationMethodInfo.GetStatements(this),
            CodeGenerationMethodInfo.GetHandlesExpressions(this));

        return result;
    }

    public override int Arity => this.TypeParameters.Length;

    public override bool ReturnsVoid
        => this.ReturnType == null || this.ReturnType.SpecialType == SpecialType.System_Void;

    public override bool ReturnsByRef
    {
        get
        {
            return RefKind == RefKind.Ref;
        }
    }

    public override bool ReturnsByRefReadonly
    {
        get
        {
            return RefKind == RefKind.RefReadOnly;
        }
    }

    public override RefKind RefKind { get; }

    public override ImmutableArray<ITypeSymbol> TypeArguments
        => this.TypeParameters.As<ITypeSymbol>();

    public override IMethodSymbol ConstructedFrom => this;

    public override bool IsReadOnly => Modifiers.IsReadOnly;
    public override bool IsInitOnly { get; }

    public override System.Reflection.MethodImplAttributes MethodImplementationFlags => default;

    public override IMethodSymbol OverriddenMethod => null;

    public override IMethodSymbol ReducedFrom => null;

    public override ITypeSymbol GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
        => throw new InvalidOperationException();

    public override IMethodSymbol ReduceExtensionMethod(ITypeSymbol receiverType)
        => null;

    public override IMethodSymbol PartialImplementationPart => null;

    public override IMethodSymbol PartialDefinitionPart => null;

    public override bool IsPartialDefinition => false;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal abstract class CodeGenerationNamespaceOrTypeSymbol : CodeGenerationSymbol, ICodeGenerationNamespaceOrTypeSymbol
{
    protected CodeGenerationNamespaceOrTypeSymbol(
        IAssemblySymbol containingAssembly,
        INamedTypeSymbol containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        string name)
        : base(containingAssembly, containingType, attributes, declaredAccessibility, modifiers, name)
    {
    }

    public virtual ImmutableArray<ISymbol> GetMembers()
        => [];

    public ImmutableArray<ISymbol> GetMembers(string name)
        => GetMembers().WhereAsArray(s => s.Name == name);

    public virtual ImmutableArray<INamedTypeSymbol> GetTypeMembers()
        => [];

    public ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name)
        => GetTypeMembers().WhereAsArray(s => s.Name == name);

    public ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity)
        => GetTypeMembers(name).WhereAsArray(n => n.Arity == arity);

    public abstract bool IsNamespace { get; }

    public abstract bool IsType { get; }
}

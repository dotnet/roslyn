// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal abstract class CodeGenerationNamespaceSymbol(string name, IList<INamespaceOrTypeSymbol> members) : CodeGenerationNamespaceOrTypeSymbol(null, null, default, Accessibility.NotApplicable, default, name), ICodeGenerationNamespaceSymbol
{
    private readonly IList<INamespaceOrTypeSymbol> _members = members ?? SpecializedCollections.EmptyList<INamespaceOrTypeSymbol>();

    public override bool IsNamespace => true;

    public override bool IsType => false;

    protected override CodeGenerationSymbol Clone()
        => (CodeGenerationSymbol)CodeGenerationSymbolMappingFactory.Instance.CreateNamespaceSymbol(this.Name, _members);

    public override SymbolKind Kind => SymbolKind.Namespace;

    public override void Accept(SymbolVisitor visitor)
        => visitor.VisitNamespace((INamespaceSymbol)this);

    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        => visitor.VisitNamespace((INamespaceSymbol)this);

    public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        => visitor.VisitNamespace((INamespaceSymbol)this, argument);

    public new IEnumerable<INamespaceOrTypeSymbol> GetMembers()
        => _members;

    IEnumerable<INamespaceOrTypeSymbol> ICodeGenerationNamespaceSymbol.GetMembers(string name)
        => GetMembers().Where(m => m.Name == name);

    public IEnumerable<INamespaceSymbol> GetNamespaceMembers()
        => GetMembers().OfType<INamespaceSymbol>();

    public bool IsGlobalNamespace
    {
        get
        {
            return this.Name == string.Empty;
        }
    }

    public NamespaceKind NamespaceKind => NamespaceKind.Module;

    public Compilation ContainingCompilation => null;

    public static INamedTypeSymbol ImplicitType => null;

    public ImmutableArray<INamespaceSymbol> ConstituentNamespaces
    {
        get
        {
            return [(INamespaceSymbol)this];
        }
    }
}

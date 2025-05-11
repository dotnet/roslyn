// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal abstract class AbstractEntryPointFinder(Compilation compilation) : SymbolVisitor
{
    protected readonly HashSet<INamedTypeSymbol> EntryPoints = [];

    private readonly KnownTaskTypes _knownTaskTypes = new(compilation);

    protected abstract bool MatchesMainMethodName(string name);

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
            member.Accept(this);
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
            member.Accept(this);
    }

    public override void VisitMethod(IMethodSymbol symbol)
    {
        // Similar to the form `static void Main(string[] args)` (and varying permutations).
        if (symbol.IsStatic &&
            MatchesMainMethodName(symbol.Name) &&
            HasValidReturnType(symbol) &&
            symbol.Parameters is [{ Type: IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_String } }] or [])
        {
            EntryPoints.Add(symbol.ContainingType);
        }
    }

    private bool HasValidReturnType(IMethodSymbol symbol)
    {
        // void
        if (symbol.ReturnsVoid)
            return true;

        var returnType = symbol.ReturnType;

        // int
        if (returnType.SpecialType == SpecialType.System_Int32)
            return true;

        // Task or ValueTask
        // Task<int> or ValueTask<int>
        return _knownTaskTypes.IsTaskLike(returnType) &&
            returnType.GetTypeArguments() is [] or [{ SpecialType: SpecialType.System_Int32 }];
    }
}

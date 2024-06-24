// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal abstract class AbstractEntryPointFinder : SymbolVisitor
{
    protected readonly HashSet<INamedTypeSymbol> EntryPoints = [];

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            member.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            member.Accept(this);
        }
    }

    public override void VisitMethod(IMethodSymbol symbol)
    {
        // named Main
        if (!MatchesMainMethodName(symbol.Name))
        {
            return;
        }

        // static
        if (!symbol.IsStatic)
        {
            return;
        }

        // returns void or int
        if (!symbol.ReturnsVoid && symbol.ReturnType.SpecialType != SpecialType.System_Int32)
        {
            return;
        }

        // parameterless or takes a string[]
        if (symbol.Parameters.Length == 1)
        {
            var parameter = symbol.Parameters.Single();
            if (parameter.Type is IArrayTypeSymbol)
            {
                var elementType = ((IArrayTypeSymbol)parameter.Type).ElementType;
                var specialType = elementType.SpecialType;

                if (specialType == SpecialType.System_String)
                {
                    EntryPoints.Add(symbol.ContainingType);
                }
            }
        }

        if (!symbol.Parameters.Any())
        {
            EntryPoints.Add(symbol.ContainingType);
        }
    }

    protected abstract bool MatchesMainMethodName(string name);
}

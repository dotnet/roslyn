// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract class AbstractEntryPointFinder : SymbolVisitor
    {
        protected readonly HashSet<INamedTypeSymbol> EntryPoints = new();

        protected virtual bool ShouldCheckEntryPoint() => true;

        protected abstract bool IsEntryPoint(IMethodSymbol methodSymbol);

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
            if (!ShouldCheckEntryPoint())
            {
                return;
            }

            if (IsEntryPoint(symbol))
            {
                EntryPoints.Add(symbol.ContainingType);
            }
        }
    }
}

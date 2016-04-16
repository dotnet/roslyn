// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        protected readonly HashSet<INamedTypeSymbol> EntryPoints = new HashSet<INamedTypeSymbol>();

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
}

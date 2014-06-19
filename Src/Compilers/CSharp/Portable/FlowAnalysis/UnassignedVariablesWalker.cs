// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An analysis that computes the set of variables that may be used
    /// before being assigned anywhere within a method.
    /// </summary>
    class UnassignedVariablesWalker : DataFlowPass
    {
        UnassignedVariablesWalker(CSharpCompilation compilation, Symbol member, BoundNode node, EmptyStructTypeCache emptyStructCache = null)
            : base(compilation, member, node, emptyStructCache)
        {
        }

        internal static HashSet<Symbol> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, EmptyStructTypeCache emptyStructCache = null)
        {
            var walker = new UnassignedVariablesWalker(compilation, member, node, emptyStructCache);
            try
            {
                bool badRegion = false;
                var result = walker.Analyze(ref badRegion);
                return badRegion ? new HashSet<Symbol>() : result;
            }
            finally
            {
                walker.Free();
            }
        }

        private readonly HashSet<Symbol> result = new HashSet<Symbol>();

        new HashSet<Symbol> Analyze(ref bool badRegion)
        {
            base.Analyze(ref badRegion, null);
            return result;
        }

        protected override void ReportUnassigned(Symbol symbol, CSharpSyntaxNode node)
        {
            // TODO: how to handle fields of structs?
            if (symbol.Kind != SymbolKind.Field)
            {
                result.Add(symbol);
            }
        }

        protected override void ReportUnassignedOutParameter(ParameterSymbol parameter, CSharpSyntaxNode node, Location location)
        {
            result.Add(parameter);
            base.ReportUnassignedOutParameter(parameter, node, location);
        }

        protected override void ReportUnassigned(FieldSymbol fieldSymbol, int unassignedSlot, CSharpSyntaxNode node)
        {
            Symbol variable = GetNonFieldSymbol(unassignedSlot);
            if ((object)variable != null) result.Add(variable);
            base.ReportUnassigned(fieldSymbol, unassignedSlot, node);
        }
    }
}
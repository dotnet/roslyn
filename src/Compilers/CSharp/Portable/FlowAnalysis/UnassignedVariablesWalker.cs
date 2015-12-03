// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal class UnassignedVariablesWalker : DataFlowPass
    {
        private UnassignedVariablesWalker(CSharpCompilation compilation, Symbol member, BoundNode node)
            : base(compilation, member, node, new NeverEmptyStructTypeCache())
        {
        }

        internal static HashSet<Symbol> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node,
                                                bool convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = false)
        {
            var walker = new UnassignedVariablesWalker(compilation, member, node);

            if (convertInsufficientExecutionStackExceptionToCancelledByStackGuardException)
            {
                walker._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = true;
            }

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

        private readonly HashSet<Symbol> _result = new HashSet<Symbol>();

        private new HashSet<Symbol> Analyze(ref bool badRegion)
        {
            base.Analyze(ref badRegion, null);
            return _result;
        }

        protected override void ReportUnassigned(Symbol symbol, CSharpSyntaxNode node)
        {
            // TODO: how to handle fields of structs?
            if (symbol.Kind != SymbolKind.Field)
            {
                _result.Add(symbol);
            }
        }

        protected override void ReportUnassignedOutParameter(ParameterSymbol parameter, CSharpSyntaxNode node, Location location)
        {
            _result.Add(parameter);
            base.ReportUnassignedOutParameter(parameter, node, location);
        }

        protected override void ReportUnassigned(FieldSymbol fieldSymbol, int unassignedSlot, CSharpSyntaxNode node)
        {
            Symbol variable = GetNonMemberSymbol(unassignedSlot);
            if ((object)variable != null) _result.Add(variable);
            base.ReportUnassigned(fieldSymbol, unassignedSlot, node);
        }
    }
}

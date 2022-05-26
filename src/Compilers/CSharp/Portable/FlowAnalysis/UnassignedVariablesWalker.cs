// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An analysis that computes the set of variables that may be used
    /// before being assigned anywhere within a method.
    /// </summary>
    internal class UnassignedVariablesWalker : DefiniteAssignmentPass
    {
        private UnassignedVariablesWalker(CSharpCompilation compilation, Symbol member, BoundNode node)
            : base(compilation, member, node, EmptyStructTypeCache.CreateNeverEmpty())
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

        private HashSet<Symbol> Analyze(ref bool badRegion)
        {
            base.Analyze(ref badRegion, null);
            return _result;
        }

        protected override void ReportUnassigned(Symbol symbol, SyntaxNode node, int slot, bool skipIfUseBeforeDeclaration)
        {
            // TODO: how to handle fields of structs?
            if (symbol.Kind != SymbolKind.Field)
            {
                _result.Add(symbol);
            }
            else
            {
                _result.Add(GetNonMemberSymbol(slot));
                base.ReportUnassigned(symbol, node, slot, skipIfUseBeforeDeclaration);
            }
        }

        protected override void ReportUnassignedOutParameter(ParameterSymbol parameter, SyntaxNode node, Location location)
        {
            _result.Add(parameter);
            base.ReportUnassignedOutParameter(parameter, node, location);
        }
    }
}
